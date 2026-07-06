<script setup lang="ts">
import { computed, onMounted, ref, type CSSProperties } from 'vue';
import {
  Activity,
  Color,
  DataPointType,
  type ActiveHoursInsight,
  type ActivitySessions,
  type ChartDataPoint,
  type PieChartDataPoint,
  type TimeActive
} from '../utils/retrospection/types';
import {
  ACTIVITY_LABELS,
  getActivityGroupFromActivityName,
  msToDecimalHours,
  getTailwindClassFromActivity
} from '../utils/retrospection/utils';
import StackedBarChart from '../components/StackedBarChart.vue';
import SelfReportTimelineChart from '../components/SelfReportTimelineChart.vue';
import type ExperienceSamplingDto from '../../shared/dto/ExperienceSamplingDto';
import studyConfig from '../../shared/study.config';

const typedIpcRenderer = window.ipcRenderer;
const isLoading = ref(false);
const selectedDay = ref(new Date());
const allWindowActivities = ref<ActivitySessions[]>([]);
const chartDataWindowActivities = ref<ChartDataPoint[]>();
const longestTimeActive = ref<TimeActive | undefined>(undefined);
const activeHoursInsight = ref<ActiveHoursInsight | undefined>(undefined);
const topApps = ref<ActivitySessions[] | undefined>(undefined);
const topWebsites = ref<ActivitySessions[]>([]);
const topWindowTitles = ref<ActivitySessions[]>([]);
const selfReports = ref<ExperienceSamplingDto[]>([]);
const ACTIVITY_BREAKDOWN_COVERAGE = 0.9;
const ACTIVITY_BREAKDOWN_MAX_ITEMS = 6;
const EXCLUDED_ACTIVITY_BREAKDOWN_GROUPS = new Set(['Other', 'Unknown']);
const experienceSamplingEnabled = studyConfig.trackers.experienceSamplingTracker.enabled;
const isWindowActivityTrackerEnabled = studyConfig.trackers.windowActivityTracker.enabled;

interface ActivityBreakdownDataPoint extends PieChartDataPoint {
  percentage: number;
}

const earliestUserComputerActivity = computed((): number => {
  return (
    chartDataWindowActivities.value?.reduce((acc, activity) => {
      if (activity.start.getTime() < acc) {
        return activity.start.getTime();
      }
      return acc;
    }, Number.MAX_SAFE_INTEGER) ?? Number.MAX_SAFE_INTEGER
  );
});

const latestUserComputerActivity = computed((): number => {
  return (
    chartDataWindowActivities.value?.reduce((acc, activity) => {
      if (!activity.end) {
        return Date.now();
      }
      if (activity.end.getTime() > acc) {
        return activity.end.getTime();
      }
      return acc;
    }, 0) ?? 0
  );
});

const topItemsAvailable = computed((): boolean => {
  return topWebsites.value.length > 0 || topWindowTitles.value.length > 0;
});

const hasActivityData = computed((): boolean => {
  return (chartDataWindowActivities.value?.length ?? 0) > 0;
});

const hasLikertSelfReports = computed((): boolean => {
  return selfReports.value.some((report) => {
    return (
      report.answerType === 'LikertScale' &&
      !report.skipped &&
      report.response !== null &&
      report.scale !== null &&
      Number.isFinite(Number(report.response))
    );
  });
});

const hasRetrospectionData = computed((): boolean => {
  return hasActivityData.value || hasLikertSelfReports.value;
});

const earliestSelfReport = computed((): number => {
  return selfReports.value.reduce((acc, report) => {
    const promptedAt = new Date(report.promptedAt).getTime();
    return promptedAt < acc ? promptedAt : acc;
  }, Number.MAX_SAFE_INTEGER);
});

const latestSelfReport = computed((): number => {
  return selfReports.value.reduce((acc, report) => {
    const promptedAt = new Date(report.promptedAt).getTime();
    return promptedAt > acc ? promptedAt : acc;
  }, 0);
});

const timelineStartDate = computed((): number => {
  const candidates = [
    hasActivityData.value ? earliestUserComputerActivity.value : null,
    hasLikertSelfReports.value ? earliestSelfReport.value : null
  ].filter((value): value is number => value !== null && Number.isFinite(value));
  return Math.min(...candidates);
});

const timelineEndDate = computed((): number => {
  const candidates = [
    hasActivityData.value ? latestUserComputerActivity.value : null,
    hasLikertSelfReports.value ? latestSelfReport.value : null
  ].filter((value): value is number => value !== null && Number.isFinite(value));
  return Math.max(...candidates);
});

const selfReportTimelineStartDate = computed((): number => {
  return hasActivityData.value ? earliestUserComputerActivity.value : timelineStartDate.value;
});

const selfReportTimelineEndDate = computed((): number => {
  return hasActivityData.value ? latestUserComputerActivity.value : timelineEndDate.value;
});

// Total tracked activity time is the denominator for percentages and the 90% cutoff.
const activityBreakdownTotalMs = computed((): number => {
  return (
    allWindowActivities.value?.reduce((total, activitySession) => {
      return total + activitySession.totalDurationMs;
    }, 0) ?? 0
  );
});

// Groups raw activities into the same activity categories and colors used by the timeline.
const activityBreakdownData = computed((): ActivityBreakdownDataPoint[] => {
  const totalDurationMs = activityBreakdownTotalMs.value;
  if (!totalDurationMs) {
    return [];
  }

  const groupTotals = new Map<string, number>();
  allWindowActivities.value.forEach((activitySession) => {
    const activityGroup = getActivityGroupFromActivityName(activitySession.type);
    if (
      EXCLUDED_ACTIVITY_BREAKDOWN_GROUPS.has(activityGroup) ||
      EXCLUDED_ACTIVITY_BREAKDOWN_GROUPS.has(activitySession.type)
    ) {
      return;
    }
    groupTotals.set(
      activityGroup,
      (groupTotals.get(activityGroup) ?? 0) + activitySession.totalDurationMs
    );
  });

  const sortedActivities = Array.from(groupTotals.entries())
    .map(([activityGroup, value]) => {
      const colorKey = getTailwindClassFromActivity(activityGroup, true) as keyof typeof Color;
      return {
        name: ACTIVITY_LABELS[activityGroup],
        value,
        color: Color[colorKey],
        type: activityGroup,
        percentage: value / totalDurationMs
      };
    })
    .filter((activity) => activity.value > 0)
    .sort((a, b) => b.value - a.value);

  let includedDurationMs = 0;
  const visibleActivities: ActivityBreakdownDataPoint[] = [];

  for (const activity of sortedActivities) {
    if (
      visibleActivities.length >= ACTIVITY_BREAKDOWN_MAX_ITEMS ||
      includedDurationMs / totalDurationMs >= ACTIVITY_BREAKDOWN_COVERAGE
    ) {
      break;
    }
    visibleActivities.push(activity);
    includedDurationMs += activity.value;
  }

  return visibleActivities;
});

const activityBreakdownTitle = computed((): string => {
  const shownActivities = activityBreakdownData.value.length;
  return shownActivities
    ? `Activity Breakdown (top ${shownActivities} activities)`
    : 'Activity Breakdown';
});

// Builds the donut from visible activities and leaves any dropped tail as a neutral segment.
const activityBreakdownStyle = computed((): CSSProperties => {
  if (!activityBreakdownData.value.length) {
    return {};
  }

  let offset = 0;
  const segments = activityBreakdownData.value.map((activity) => {
    const start = offset;
    offset += activity.percentage * 360;
    return `${activity.color} ${start}deg ${offset}deg`;
  });

  if (offset < 360) {
    segments.push(`${Color['neutral-200']} ${offset}deg 360deg`);
  }

  return {
    background: `conic-gradient(${segments.join(', ')})`
  };
});

onMounted(async () => {
  await loadData();
});

// selectedDay is a calendar date; the main process resolves it to the 04:00-to-04:00 local
// workday (getRetrospectionWorkdayRange), so work before 04:00 belongs to the previous day.
// Every data query and visualization for a day must apply that same range uniformly.
async function loadData() {
  isLoading.value = true;
  await loadActiveHours();
  await loadLongestTimeActive();
  await loadMostActiveApps();
  await loadTopWebsites();
  await loadTopWindowTitles();
  await loadSelfReports();
  await loadWindowActivities();
  isLoading.value = false;
}

function windowActivitiesToChartData() {
  const dataPoints: ChartDataPoint[] = [];

  allWindowActivities.value?.forEach((activitySession: ActivitySessions) => {
    activitySession.sessions.forEach((session: TimeActive) => {
      dataPoints.push({
        type: DataPointType.WINDOW_ACTIVITY,
        activity: activitySession.type as Activity,
        start: session.from,
        end: session.to,
        color: getTailwindClassFromActivity(activitySession.type),
        details: session.details,
        hiddenDetailCount: session.hiddenDetailCount
      });
    });
  });

  chartDataWindowActivities.value = dataPoints;
}

async function loadWindowActivities() {
  allWindowActivities.value = (await typedIpcRenderer.invoke(
    'retrospectionGetActivities',
    selectedDay.value
  )) as ActivitySessions[];
  windowActivitiesToChartData();
}

async function loadActiveHours() {
  try {
    activeHoursInsight.value = (await typedIpcRenderer.invoke(
      'retrospectionGetActiveHours',
      selectedDay.value
    )) as ActiveHoursInsight;
  } catch (error) {
    console.error('Error loading active hours', error);
  }
}

async function loadLongestTimeActive() {
  try {
    longestTimeActive.value = (await typedIpcRenderer.invoke(
      'retrospectionLoadLongestTimeActive',
      selectedDay.value
    )) as TimeActive;
  } catch (error) {
    console.error('Error loading longest time active', error);
  }
}

async function loadMostActiveApps() {
  try {
    topApps.value = (await typedIpcRenderer.invoke(
      'retrospectionGetTopThreeMostActiveApps',
      selectedDay.value
    )) as ActivitySessions[];
  } catch (error) {
    console.error('Error loading most active apps', error);
  }
}

async function loadTopWebsites() {
  try {
    topWebsites.value = (await typedIpcRenderer.invoke(
      'retrospectionGetTopThreeWebsites',
      selectedDay.value
    )) as ActivitySessions[];
  } catch (error) {
    console.error('Error loading top websites', error);
  }
}

async function loadTopWindowTitles() {
  try {
    topWindowTitles.value = (await typedIpcRenderer.invoke(
      'retrospectionGetTopThreeWindowTitles',
      selectedDay.value
    )) as ActivitySessions[];
  } catch (error) {
    console.error('Error loading top window titles', error);
  }
}

async function loadSelfReports() {
  if (!experienceSamplingEnabled) {
    selfReports.value = [];
    return;
  }
  try {
    selfReports.value = (await typedIpcRenderer.invoke(
      'retrospectionGetSelfReports',
      selectedDay.value
    )) as ExperienceSamplingDto[];
  } catch (error) {
    selfReports.value = [];
    console.error('Error loading self reports', error);
  }
}

function msToMinutes(ms: number): number {
  return Math.round(ms / 60000);
}

function renderTime(ms: number): string {
  let minutes = msToMinutes(ms);
  if (minutes < 60) {
    return `${minutes} minutes`;
  }

  const hours = Math.floor(minutes / 60);
  minutes = minutes % 60;
  if (minutes === 0) {
    if (hours === 1) {
      return `${hours} hour`;
    } else {
      return `${hours} hours`;
    }
  } else {
    const fractionalHours = Math.round((minutes / 60) * 10) / 10;
    return `${hours + fractionalHours} hours`;
  }
}

function renderCompactTime(ms: number): string {
  const minutes = msToMinutes(ms);
  if (minutes < 1) {
    return '< 1 min';
  }
  if (minutes < 60) {
    return `${minutes} min`;
  }

  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;
  return remainingMinutes ? `${hours} hr ${remainingMinutes} min` : `${hours} hr`;
}

function getTopItemWidth(item: ActivitySessions, items: ActivitySessions[]): string {
  const maxDurationMs = Math.max(...items.map((topItem) => topItem.totalDurationMs), 1);
  return `${Math.max((item.totalDurationMs / maxDurationMs) * 100, 8)}%`;
}

function getTopItemColor(item: ActivitySessions): string {
  const activity = item.activity || Activity.Other;
  const colorKey = getTailwindClassFromActivity(activity) as keyof typeof Color;
  return Color[colorKey] || Color['neutral-400'];
}

async function handleDayChange(event: Event) {
  const value = (event.target as HTMLInputElement).value;
  if (!value) return;
  selectedDay.value = parseDateInputValue(value);
  await loadData();
}

function parseDateInputValue(value: string): Date {
  const [year, month, day] = value.split('-').map(Number);
  return new Date(year, month - 1, day);
}

function formatDateInputValue(date: Date): string {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  return `${year}-${month}-${day}`;
}

const isToday = computed(() => {
  const today = new Date();
  return (
    selectedDay.value.getDate() === today.getDate() &&
    selectedDay.value.getMonth() === today.getMonth() &&
    selectedDay.value.getFullYear() === today.getFullYear()
  );
});

async function navigateToPreviousDay() {
  const newDate = new Date(selectedDay.value);
  newDate.setDate(newDate.getDate() - 1);
  selectedDay.value = newDate;
  await loadData();
}

async function navigateToNextDay() {
  if (isToday.value) return;
  const newDate = new Date(selectedDay.value);
  newDate.setDate(newDate.getDate() + 1);
  selectedDay.value = newDate;
  await loadData();
}

async function navigateToToday() {
  if (isToday.value) return;
  selectedDay.value = new Date();
  await loadData();
}

function getNearestFullHourTime(time: number, offset: number): number {
  const nextFullHour = new Date(time);
  nextFullHour.setHours(nextFullHour.getHours() + offset, 0, 0, 0);
  return nextFullHour.getTime();
}

function getTimeString(date: Date | string | number): string {
  const d = new Date(date);
  const hours = d.getHours().toString().padStart(2, '0');
  const minutes = d.getMinutes().toString().padStart(2, '0');
  return `${hours}:${minutes}`;
}

function getDayLabel(date: Date): string {
  const today = new Date();
  const yesterday = new Date();
  yesterday.setDate(yesterday.getDate() - 1);

  if (
    date.getDate() === today.getDate() &&
    date.getMonth() === today.getMonth() &&
    date.getFullYear() === today.getFullYear()
  ) {
    return 'Today';
  } else if (
    date.getDate() === yesterday.getDate() &&
    date.getMonth() === yesterday.getMonth() &&
    date.getFullYear() === yesterday.getFullYear()
  ) {
    return 'Yesterday';
  } else {
    return date.toLocaleDateString(undefined, {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }
}
</script>

<template>
  <!-- No data for this day -->
  <template v-if="!isLoading && !hasRetrospectionData">
    <div class="flex h-screen items-center justify-center">
      <!-- day picker -->
      <div class="absolute right-6 top-6 z-10 flex items-center gap-1">
        <button
          :disabled="isToday"
          aria-label="Show today"
          class="h-8 rounded border border-gray-300 bg-white px-3 text-sm text-gray-800 disabled:opacity-40 dark:border-neutral-600 dark:bg-neutral-700 dark:text-slate-200"
          title="Show today"
          @click="navigateToToday"
        >
          Today
        </button>
        <button
          aria-label="Show previous day"
          class="h-8 rounded border border-gray-300 bg-white px-2 text-gray-800 dark:border-neutral-600 dark:bg-neutral-700 dark:text-slate-200"
          title="Show previous day"
          @click="navigateToPreviousDay"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2.5"
            stroke-linecap="round"
            stroke-linejoin="round"
            class="h-4 w-4"
          >
            <polyline points="15 18 9 12 15 6" />
          </svg>
        </button>
        <input
          type="date"
          :value="formatDateInputValue(selectedDay)"
          :max="formatDateInputValue(new Date())"
          class="h-8 rounded border border-gray-300 bg-white px-2 text-gray-800 dark:border-neutral-600 dark:bg-neutral-700 dark:text-slate-200"
          style="min-width: 140px"
          @change="handleDayChange"
        />
        <button
          :disabled="isToday"
          aria-label="Show next day"
          class="h-8 rounded border border-gray-300 bg-white px-2 text-gray-800 disabled:opacity-40 dark:border-neutral-600 dark:bg-neutral-700 dark:text-slate-200"
          title="Show next day"
          @click="navigateToNextDay"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2.5"
            stroke-linecap="round"
            stroke-linejoin="round"
            class="h-4 w-4"
          >
            <polyline points="9 18 15 12 9 6" />
          </svg>
        </button>
      </div>
      <div class="text-center text-gray-800 dark:text-gray-200">
        <template v-if="isWindowActivityTrackerEnabled">
          <h1 class="mb-8 text-2xl font-bold">No data for this day.</h1>
          <span class="text-gray-600 dark:text-gray-400"
            >There is no data recorded for this date. Please select a different day.</span
          >
        </template>
        <template v-else>
          <h1 class="mb-8 text-2xl font-bold">Retrospection data is not available.</h1>
          <span class="text-gray-600 dark:text-gray-400"
            >Window activity tracking is disabled for this study.</span
          >
        </template>
      </div>
    </div>
  </template>

  <!-- Retrospection dashboard -->
  <template v-else>
    <div class="view flex h-screen flex-col overflow-y-auto">
      <!-- day picker -->
      <div class="absolute right-6 top-6 z-10 flex items-center gap-1">
        <button
          :disabled="isToday"
          aria-label="Show today"
          class="h-8 rounded border border-gray-300 bg-white px-3 text-sm text-gray-800 disabled:opacity-40 dark:border-neutral-600 dark:bg-neutral-700 dark:text-slate-200"
          title="Show today"
          @click="navigateToToday"
        >
          Today
        </button>
        <button
          aria-label="Show previous day"
          class="h-8 rounded border border-gray-300 bg-white px-2 text-gray-800 dark:border-neutral-600 dark:bg-neutral-700 dark:text-slate-200"
          title="Show previous day"
          @click="navigateToPreviousDay"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2.5"
            stroke-linecap="round"
            stroke-linejoin="round"
            class="h-4 w-4"
          >
            <polyline points="15 18 9 12 15 6" />
          </svg>
        </button>
        <input
          type="date"
          :value="formatDateInputValue(selectedDay)"
          :max="formatDateInputValue(new Date())"
          class="h-8 rounded border border-gray-300 bg-white px-2 text-gray-800 dark:border-neutral-600 dark:bg-neutral-700 dark:text-slate-200"
          style="min-width: 140px"
          @change="handleDayChange"
        />
        <button
          :disabled="isToday"
          aria-label="Show next day"
          class="h-8 rounded border border-gray-300 bg-white px-2 text-gray-800 disabled:opacity-40 dark:border-neutral-600 dark:bg-neutral-700 dark:text-slate-200"
          title="Show next day"
          @click="navigateToNextDay"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2.5"
            stroke-linecap="round"
            stroke-linejoin="round"
            class="h-4 w-4"
          >
            <polyline points="9 18 15 12 9 6" />
          </svg>
        </button>
      </div>

      <div>
        <h1 class="primary-blue mb-3 text-2xl font-bold">
          {{ getDayLabel(selectedDay) }} in Review
        </h1>
        <div class="subline mb-8 text-gray-600 dark:text-gray-400">
          Take a moment to reflect on your workday.
        </div>

        <!-- Timeline Visualization -->
        <template v-if="hasActivityData">
          <h1 class="mb-2 mt-8 text-xl font-bold text-gray-900 dark:text-gray-100">
            Activities over time
          </h1>
          <StackedBarChart
            v-if="!isLoading && chartDataWindowActivities"
            :data="chartDataWindowActivities"
            :start-date="getNearestFullHourTime(earliestUserComputerActivity, 0)"
            :end-date="getNearestFullHourTime(latestUserComputerActivity, 1)"
            type="WINDOW_ACTIVITY"
          />
        </template>

        <!-- Info Tiles -->
        <h1
          v-if="hasActivityData"
          class="mb-2 mt-8 text-xl font-bold text-gray-900 dark:text-gray-100"
        >
          Insights of your day
        </h1>
        <div v-if="hasActivityData" class="tile-grid">
          <!-- Tile 1: Longest active period -->
          <div
            v-if="longestTimeActive"
            class="rounded border border-gray-200 bg-gray-100 px-4 py-3 text-gray-800 dark:border-transparent dark:bg-neutral-800 dark:text-slate-200"
          >
            <h2 class="primary-blue font-bold leading-4">Longest active period</h2>
            <p class="mt-2">
              Your longest active streak was
              <b>{{ renderTime(longestTimeActive!.duration * 60000) }}</b> (between
              {{ getTimeString(longestTimeActive!.from) }} and
              {{ getTimeString(longestTimeActive!.to) }}).
            </p>
          </div>

          <!-- Tile 2: Active hours -->
          <div
            v-if="chartDataWindowActivities?.length"
            class="rounded border border-gray-200 bg-gray-100 px-4 py-3 text-gray-800 dark:border-transparent dark:bg-neutral-800 dark:text-slate-200"
          >
            <h2 class="primary-blue font-bold leading-4">Active hours</h2>
            <dl class="mt-2 space-y-1">
              <div>
                <dt class="inline">Active on the computer:</dt>
                <dd class="ml-1 inline">
                  <b>{{ msToDecimalHours(activeHoursInsight?.activeDurationMs ?? 0) }}</b>
                </dd>
              </div>
              <div>
                <dt class="inline">Spanning work:</dt>
                <dd class="ml-1 inline">
                  <b>{{
                    msToDecimalHours(latestUserComputerActivity - earliestUserComputerActivity)
                  }}</b>
                  (between {{ getTimeString(earliestUserComputerActivity) }} and
                  {{ getTimeString(latestUserComputerActivity) }})
                </dd>
              </div>
            </dl>
          </div>

          <!-- Tile 3: Top apps -->
          <div
            v-if="topApps?.length"
            class="rounded border border-gray-200 bg-gray-100 px-4 py-3 text-gray-800 dark:border-transparent dark:bg-neutral-800 dark:text-slate-200"
          >
            <h2 class="primary-blue font-bold leading-4">Top apps</h2>
            <ol class="mt-2 list-decimal pl-4">
              <li v-for="(appSession, index) in topApps" :key="index">
                {{ appSession.type }}: {{ renderTime(appSession.totalDurationMs) }}
              </li>
            </ol>
          </div>

          <!-- Tile 4: Activity breakdown -->
          <div
            v-if="activityBreakdownData.length"
            class="activity-breakdown-card rounded border border-gray-200 bg-gray-100 px-4 py-3 text-gray-800 dark:border-transparent dark:bg-neutral-800 dark:text-slate-200"
          >
            <h2 class="primary-blue font-bold leading-4">{{ activityBreakdownTitle }}</h2>
            <div class="activity-breakdown-content">
              <div class="activity-pie" :style="activityBreakdownStyle" aria-hidden="true">
                <div class="activity-pie-hole"></div>
              </div>
              <ol class="activity-breakdown-list">
                <li
                  v-for="activity in activityBreakdownData"
                  :key="activity.type"
                  :title="`${activity.name}: ${renderCompactTime(activity.value)}`"
                >
                  <span class="activity-breakdown-label">
                    <span
                      class="activity-breakdown-dot"
                      :style="{ backgroundColor: activity.color }"
                    ></span>
                    <span class="activity-breakdown-name">{{ activity.name }}</span>
                  </span>
                  <span class="activity-breakdown-time text-slate-700 dark:!text-slate-100">{{
                    renderCompactTime(activity.value)
                  }}</span>
                </li>
              </ol>
            </div>
          </div>
        </div>

        <div v-if="hasActivityData && topItemsAvailable" class="top-item-grid tile-grid">
          <div
            v-if="topWebsites.length"
            class="top-item-card rounded border border-gray-200 bg-gray-100 px-4 py-3 text-gray-800 dark:border-transparent dark:bg-neutral-800 dark:text-slate-200"
          >
            <h2 class="primary-blue font-bold leading-4">Top websites</h2>
            <ol class="top-item-list">
              <li
                v-for="website in topWebsites"
                :key="website.type"
                class="top-item-row"
                :title="website.tooltipTitle || website.type"
              >
                <div class="top-item-content">
                  <span class="top-item-label">{{ website.type }}</span>
                  <span class="top-item-time">{{
                    renderCompactTime(website.totalDurationMs)
                  }}</span>
                </div>
                <div class="top-item-track">
                  <div
                    class="top-item-bar"
                    :style="{
                      width: getTopItemWidth(website, topWebsites),
                      backgroundColor: getTopItemColor(website)
                    }"
                  ></div>
                </div>
              </li>
            </ol>
          </div>

          <div
            v-if="topWindowTitles.length"
            class="top-item-card rounded border border-gray-200 bg-gray-100 px-4 py-3 text-gray-800 dark:border-transparent dark:bg-neutral-800 dark:text-slate-200"
          >
            <h2 class="primary-blue font-bold leading-4">Top window titles</h2>
            <ol class="top-item-list">
              <li
                v-for="windowTitle in topWindowTitles"
                :key="windowTitle.type"
                class="top-item-row"
                :title="windowTitle.tooltipTitle || windowTitle.type"
              >
                <div class="top-item-content">
                  <span class="top-item-label">{{ windowTitle.type }}</span>
                  <span class="top-item-time">{{
                    renderCompactTime(windowTitle.totalDurationMs)
                  }}</span>
                </div>
                <div class="top-item-track">
                  <div
                    class="top-item-bar"
                    :style="{
                      width: getTopItemWidth(windowTitle, topWindowTitles),
                      backgroundColor: getTopItemColor(windowTitle)
                    }"
                  ></div>
                </div>
              </li>
            </ol>
          </div>
        </div>

        <!-- Self-report Visualization -->
        <template v-if="experienceSamplingEnabled">
          <h1 class="mb-2 mt-8 text-xl font-bold text-gray-900 dark:text-gray-100">
            Self-reports over time
          </h1>
          <SelfReportTimelineChart
            v-if="!isLoading && hasLikertSelfReports"
            :data="selfReports"
            :start-date="getNearestFullHourTime(selfReportTimelineStartDate, 0)"
            :end-date="getNearestFullHourTime(selfReportTimelineEndDate, 1)"
          />
          <div
            v-else-if="!isLoading"
            class="rounded border border-gray-200 bg-gray-100 px-4 py-3 text-gray-700 dark:border-transparent dark:bg-neutral-800 dark:text-slate-300"
          >
            No self-reports were recorded for this day.
          </div>
        </template>
      </div>
    </div>
  </template>
</template>

<style lang="less" scoped>
@import '../styles/index';

h2.primary-blue {
  color: @primary-color;
}

.primary-blue {
  color: @primary-color;
}

.view {
  padding: 25px;
}

.tile-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1.2rem;
  width: 100%;
}

.top-item-grid {
  margin-top: 1.2rem;
}

.top-item-card {
  min-height: 118px;
}

.top-item-list {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  margin: 0.4rem 0 0;
  padding: 0;
  list-style: none;
}

.top-item-row {
  min-height: 0;
  padding: 0.2rem 0;
}

.top-item-content {
  display: grid;
  grid-template-columns: minmax(0, 1fr) max-content;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.15rem;
  line-height: 1.25rem;
}

.top-item-label {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: #1f2937;
  font-weight: 400;
}

.top-item-time {
  color: #374151;
  font-weight: 400;
  white-space: nowrap;
}

.top-item-track {
  height: 0.35rem;
  overflow: hidden;
  border-radius: 999px;
  background: #f3f4f6;
}

.top-item-bar {
  height: 100%;
  border-radius: inherit;
}

:global(.dark) .top-item-track {
  background: #111111;
}

:global(.dark) .top-item-label,
:global(.dark) .top-item-time {
  color: #ffffff;
}

.activity-breakdown-card {
  min-height: 128px;
}

.activity-breakdown-content {
  display: grid;
  grid-template-columns: 86px minmax(0, 1fr);
  align-items: center;
  gap: 1rem;
  margin-top: 0.75rem;
}

.activity-pie {
  position: relative;
  width: 86px;
  height: 86px;
  border-radius: 50%;
  flex: 0 0 auto;
}

.activity-pie-hole {
  position: absolute;
  inset: 20px;
  border-radius: 50%;
  background: #f3f4f6;
}

.activity-breakdown-list {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  min-width: 0;
  margin: 0;
  padding: 0;
  list-style: none;
}

.activity-breakdown-list li {
  display: grid;
  grid-template-columns: minmax(0, 1fr) max-content;
  align-items: center;
  gap: 0.75rem;
  min-width: 0;
  line-height: 1.3rem;
}

.activity-breakdown-label {
  display: flex;
  align-items: center;
  min-width: 0;
  gap: 0.45rem;
}

.activity-breakdown-dot {
  width: 0.65rem;
  height: 0.65rem;
  border-radius: 50%;
  flex: 0 0 auto;
}

.activity-breakdown-name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.activity-breakdown-time {
  white-space: nowrap;
}

:global(.dark) .activity-breakdown-time,
:global([data-theme='dark']) .activity-breakdown-time {
  color: #f1f5f9;
}

@media (prefers-color-scheme: dark) {
  .top-item-track {
    background: #111111;
  }

  .top-item-label,
  .top-item-time {
    color: #ffffff;
  }

  .activity-pie-hole {
    background: #262626;
  }

  .activity-breakdown-time {
    color: #f1f5f9;
  }
}

@media (max-width: 720px) {
  .tile-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 420px) {
  .activity-breakdown-content {
    grid-template-columns: 72px minmax(0, 1fr);
  }

  .activity-pie {
    width: 72px;
    height: 72px;
  }

  .activity-pie-hole {
    inset: 17px;
  }
}
</style>
