import { getMainLogger } from '../../config/Logger';
import type { WindowActivityEntity } from '../entities/WindowActivityEntity';
import type {
  ActiveHoursInsight,
  ActivitySessions,
  RetrospectionDataSection,
  TimeActive
} from '../../../src/utils/retrospection/types';
import {
  getWindowActivitiesForWorkday,
  loadRetrospectionWorkdayData,
  type RetrospectionWorkdayData
} from './retrospection/RetrospectionWorkdayData';
import {
  buildActiveHoursFigure,
  buildLongestActivePeriodFigure
} from './retrospection/figures/ActiveHoursFigure';
import {
  buildActivityTimelineFigure,
  isSpecificRetrospectionActivity
} from './retrospection/figures/ActivityTimelineFigure';
import { buildAppUsageFigure, buildTopAppsFigure } from './retrospection/figures/TopAppsFigure';
import { buildTopWebsitesFigure } from './retrospection/figures/TopWebsitesFigure';
import { buildTopWindowTitlesFigure } from './retrospection/figures/TopWindowTitlesFigure';

const LOG = getMainLogger('RetrospectionService');

const ACTIVITY_SECTION_ORDER: RetrospectionDataSection[] = [
  'activities',
  'activeHours',
  'longestActivePeriod',
  'topApps',
  'topWebsites',
  'topWindowTitles'
];

export interface RetrospectionActivityDashboard {
  activities: ActivitySessions[];
  activeHours?: ActiveHoursInsight;
  longestActivePeriod?: TimeActive;
  topApps: ActivitySessions[];
  topWebsites: ActivitySessions[];
  topWindowTitles: ActivitySessions[];
  errors: RetrospectionDataSection[];
}

function emptyActivityDashboard(
  errors: RetrospectionDataSection[] = []
): RetrospectionActivityDashboard {
  return {
    activities: [],
    activeHours: undefined,
    longestActivePeriod: undefined,
    topApps: [],
    topWebsites: [],
    topWindowTitles: [],
    errors
  };
}

/**
 * Builds every tracker-backed figure from one shared set of workday data.
 * A figure failure is isolated to its own section, while a data-loading failure marks all activity
 * sections unavailable because none of them can be calculated without the raw tracker data.
 */
export async function getRetrospectionActivityDashboard(
  date: Date
): Promise<RetrospectionActivityDashboard> {
  let workdayData: RetrospectionWorkdayData;
  try {
    workdayData = await loadRetrospectionWorkdayData(date);
  } catch (error) {
    LOG.error('Error loading retrospection workday data', error);
    return emptyActivityDashboard([...ACTIVITY_SECTION_ORDER]);
  }

  const errors = new Set<RetrospectionDataSection>();
  const buildFigure = async <T>(
    section: RetrospectionDataSection,
    builder: () => T | Promise<T>,
    fallback: T
  ): Promise<T> => {
    try {
      return await builder();
    } catch (error) {
      errors.add(section);
      LOG.error(`Error building retrospection figure: ${section}`, error);
      return fallback;
    }
  };

  const [activities, activeHours, longestActivePeriod, topApps, topWebsites, topWindowTitles] =
    await Promise.all([
      buildFigure('activities', () => buildActivityTimelineFigure(workdayData), []),
      buildFigure('activeHours', () => buildActiveHoursFigure(workdayData), undefined),
      buildFigure(
        'longestActivePeriod',
        () => buildLongestActivePeriodFigure(workdayData),
        undefined
      ),
      buildFigure('topApps', () => buildTopAppsFigure(workdayData), []),
      buildFigure('topWebsites', () => buildTopWebsitesFigure(workdayData), []),
      buildFigure('topWindowTitles', () => buildTopWindowTitlesFigure(workdayData), [])
    ]);

  return {
    activities,
    activeHours,
    longestActivePeriod,
    topApps,
    topWebsites,
    topWindowTitles,
    errors: ACTIVITY_SECTION_ORDER.filter((section) => errors.has(section))
  };
}

// Compatibility façade for callers and focused tests. Dashboard IPC uses the shared workday data.
export const getWindowActivities = getWindowActivitiesForWorkday;

export async function getActiveHoursInsight(date: Date): Promise<ActiveHoursInsight> {
  return buildActiveHoursFigure(await loadRetrospectionWorkdayData(date));
}

export async function getLongestTimeActiveInsight(date: Date): Promise<TimeActive> {
  return buildLongestActivePeriodFigure(await loadRetrospectionWorkdayData(date));
}

export async function getAppUsageSessions(date: Date): Promise<ActivitySessions[]> {
  return buildAppUsageFigure(await loadRetrospectionWorkdayData(date));
}

export async function getTopWebsiteSessions(date: Date, limit = 3): Promise<ActivitySessions[]> {
  return buildTopWebsitesFigure(await loadRetrospectionWorkdayData(date), limit);
}

export async function getTopWindowTitleSessions(
  date: Date,
  limit = 3
): Promise<ActivitySessions[]> {
  return buildTopWindowTitlesFigure(await loadRetrospectionWorkdayData(date), limit);
}

export async function getActivitySessions(
  date: Date,
  excludeUnspecificActivities = true
): Promise<ActivitySessions[]> {
  return await buildActivityTimelineFigure(
    await loadRetrospectionWorkdayData(date),
    excludeUnspecificActivities
  );
}

export { isSpecificRetrospectionActivity };
export {
  cleanWindowTitle,
  getReadableUrlTitle,
  isBrowserProcessName,
  removeGenericBrowserTabCountFragments,
  stripPathFragment
} from './retrospection/WindowTitle';
export type {
  ActivitySessions,
  TimeActive,
  TimelineHoverDetail
} from '../../../src/utils/retrospection/types';
export type { WindowActivityEntity };
