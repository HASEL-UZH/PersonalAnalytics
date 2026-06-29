<template>
  <div class="self-report-chart">
    <div
      ref="tooltip"
      class="self-report-tooltip rounded border border-gray-200 bg-white p-2 text-gray-700 opacity-0 shadow-lg transition-opacity duration-300 ease-in-out dark:border-transparent dark:bg-neutral-800 dark:text-neutral-300 dark:shadow-neutral-800/80"
    ></div>
    <svg ref="chart" :width="svgWidth" :height="svgHeight"></svg>
    <div v-if="series.length > 1" class="self-report-legend">
      <button
        v-for="item in series"
        :key="item.question"
        type="button"
        class="self-report-legend-item"
        :class="{
          'self-report-legend-item-muted': selectedQuestion && selectedQuestion !== item.question
        }"
        :aria-label="`Focus self-report question: ${item.question}`"
        :title="item.question"
        @click="toggleQuestion(item.question)"
      >
        <span class="self-report-legend-dot" :style="{ backgroundColor: item.color }"></span>
        <span class="self-report-legend-label">{{ item.question }}</span>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch, type PropType } from 'vue';
import * as d3 from 'd3';
import type ExperienceSamplingDto from '../../shared/dto/ExperienceSamplingDto';
import { Color } from '../utils/retrospection/types';

interface SelfReportPoint {
  id: string;
  question: string;
  promptedAt: Date;
  value: number;
  scale: number;
  normalizedValue: number;
  labels: string[];
}

interface SelfReportSeries {
  question: string;
  color: string;
  points: SelfReportPoint[];
}

interface AxisLabel {
  value: number;
  label: string;
}

const props = defineProps({
  data: {
    type: Array as PropType<ExperienceSamplingDto[]>,
    required: true
  },
  startDate: {
    type: Number,
    required: true
  },
  endDate: {
    type: Number,
    required: true
  }
});

const svgWidth = 760;
const svgHeight = 190;
const chart = ref<SVGElement | null>(null);
const tooltip = ref<HTMLElement | null>(null);
const darkMediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
const isDark = ref(darkMediaQuery.matches);
const selectedQuestion = ref<string | null>(null);

const palette = [
  Color['blue-600'],
  Color['green-600'],
  Color['rose-600'],
  Color['amber-600'],
  Color['violet-600'],
  Color['teal-600'],
  Color['orange-600'],
  Color['fuchsia-600']
];

const selfReportPoints = computed((): SelfReportPoint[] => {
  return props.data
    .filter((report) => {
      return (
        report.answerType === 'LikertScale' &&
        !report.skipped &&
        report.response !== null &&
        report.scale !== null
      );
    })
    .map((report) => {
      const value = Number(report.response);
      const scale = report.scale ?? 0;
      if (!Number.isFinite(value) || scale < 2) {
        return null;
      }
      return {
        id: report.id,
        question: report.question,
        promptedAt: new Date(report.promptedAt),
        value,
        scale,
        normalizedValue: (value - 1) / (scale - 1),
        labels: parseLikertLabels(report)
      };
    })
    .filter((point): point is SelfReportPoint => point !== null)
    .sort((a, b) => a.promptedAt.getTime() - b.promptedAt.getTime());
});

const series = computed((): SelfReportSeries[] => {
  const groups = new Map<string, SelfReportPoint[]>();
  selfReportPoints.value.forEach((point) => {
    groups.set(point.question, [...(groups.get(point.question) ?? []), point]);
  });

  return Array.from(groups.entries()).map(([question, points], index) => ({
    question,
    points,
    color: palette[index % palette.length]
  }));
});

watch([() => props.startDate, () => props.endDate, series], () => {
  if (
    selectedQuestion.value &&
    !series.value.some((item) => item.question === selectedQuestion.value)
  ) {
    selectedQuestion.value = null;
  }
  rebuildChart();
});

onMounted(() => {
  darkMediaQuery.addEventListener('change', onThemeChange);
  rebuildChart();
});

onUnmounted(() => {
  darkMediaQuery.removeEventListener('change', onThemeChange);
});

function onThemeChange(e: MediaQueryListEvent) {
  isDark.value = e.matches;
  rebuildChart();
}

function parseLikertLabels(report: ExperienceSamplingDto): string[] {
  if (!report.responseOptions) {
    return [];
  }
  try {
    const parsed = JSON.parse(report.responseOptions);
    if (parsed && typeof parsed === 'object' && Array.isArray(parsed.labels)) {
      return parsed.labels;
    }
    if (Array.isArray(parsed)) {
      return parsed;
    }
  } catch {
    return [];
  }
  return [];
}

function getLabel(point: SelfReportPoint, position: 'low' | 'mid' | 'high'): string {
  if (position === 'low') {
    return point.labels[0] || 'Low';
  }
  if (position === 'high') {
    return point.labels.at(-1) || 'High';
  }
  return point.labels.length === 3 ? point.labels[1] : 'Mid';
}

function getMidScaleValue(scale: number): number {
  return Math.ceil(scale / 2);
}

function getScaleAxisLabels(scale: number): AxisLabel[] {
  const midValue = getMidScaleValue(scale);
  return [
    { value: 0, label: '1' },
    { value: (midValue - 1) / (scale - 1), label: `${midValue}` },
    { value: 1, label: `${scale}` }
  ];
}

function haveSameLabels(a: string[], b: string[]): boolean {
  return a.length === b.length && a.every((label, index) => label === b[index]);
}

function getActualAxisLabels(point: SelfReportPoint): AxisLabel[] {
  return [
    { value: 0, label: getLabel(point, 'low') },
    { value: 0.5, label: getLabel(point, 'mid') },
    { value: 1, label: getLabel(point, 'high') }
  ];
}

function getAxisLabels(): AxisLabel[] {
  const selectedSeries = selectedQuestion.value
    ? series.value.find((item) => item.question === selectedQuestion.value)
    : null;
  const labelReferencePoint =
    selectedSeries?.points[0] ?? (series.value.length === 1 ? series.value[0].points[0] : null);
  if (labelReferencePoint) {
    return getActualAxisLabels(labelReferencePoint);
  }

  const firstPoint = series.value[0]?.points[0];
  if (!firstPoint) {
    return [
      { value: 0, label: 'Min' },
      { value: 0.5, label: 'Mid' },
      { value: 1, label: 'Max' }
    ];
  }

  const allSameScale = series.value.every((item) => item.points[0]?.scale === firstPoint.scale);
  const allSameLabels = series.value.every((item) =>
    haveSameLabels(item.points[0]?.labels ?? [], firstPoint.labels)
  );
  if (allSameScale && allSameLabels && firstPoint.labels.length > 0) {
    return getActualAxisLabels(firstPoint);
  }
  if (allSameScale) {
    return getScaleAxisLabels(firstPoint.scale);
  }

  return [
    { value: 0, label: 'Min' },
    { value: 0.5, label: 'Mid' },
    { value: 1, label: 'Max' }
  ];
}

function toggleQuestion(question: string) {
  selectedQuestion.value = selectedQuestion.value === question ? null : question;
  rebuildChart();
}

function getSeriesOpacity(question: string): number {
  return !selectedQuestion.value || selectedQuestion.value === question ? 1 : 0.15;
}

function rebuildChart() {
  if (!chart.value) {
    return;
  }

  d3.select(chart.value).selectAll('*').remove();
  buildChart();
}

function buildChart() {
  if (!chart.value) {
    return;
  }

  const margin = { top: 22, right: 0, bottom: 34, left: 0 };
  const width = svgWidth - margin.left - margin.right;
  const height = svgHeight - margin.top - margin.bottom;
  const axisColor = isDark.value ? '#a3a3a3' : '#374151';
  const gridColor = isDark.value ? '#404040' : '#e5e7eb';
  const labelColor = isDark.value ? '#d4d4d4' : '#4b5563';

  const svg = d3
    .select(chart.value)
    .append('g')
    .attr('transform', `translate(${margin.left},${margin.top})`);

  const x = d3.scaleTime().domain([props.startDate, props.endDate]).range([0, width]);
  const y = d3.scaleLinear().domain([0, 1]).range([height, 0]);
  const timeFormat = d3.timeFormat('%H:%M');

  [0, 0.5, 1].forEach((tick) => {
    svg
      .append('line')
      .attr('x1', 0)
      .attr('x2', width)
      .attr('y1', y(tick))
      .attr('y2', y(tick))
      .attr('stroke', gridColor)
      .attr('stroke-width', 1);
  });

  getAxisLabels().forEach((tick) => {
    svg
      .append('text')
      .attr('x', width - 4)
      .attr('y', y(tick.value) - 5)
      .attr('text-anchor', 'end')
      .attr('font-size', '11px')
      .attr('fill', labelColor)
      .text(tick.label);
  });

  const line = d3
    .line<SelfReportPoint>()
    .x((point) => x(point.promptedAt))
    .y((point) => y(point.normalizedValue))
    .curve(d3.curveMonotoneX);

  series.value.forEach((item) => {
    if (item.points.length > 1) {
      svg
        .append('path')
        .datum(item.points)
        .attr('fill', 'none')
        .attr('stroke', item.color)
        .attr('stroke-width', 2)
        .attr('stroke-dasharray', '5 5')
        .attr('stroke-linecap', 'round')
        .attr('opacity', getSeriesOpacity(item.question))
        .attr('d', line);
    }

    const pointGroup = svg.append('g');
    pointGroup
      .selectAll<SVGCircleElement, SelfReportPoint>('circle')
      .data(item.points)
      .enter()
      .append('circle')
      .attr('cx', (point) => x(point.promptedAt))
      .attr('cy', (point) => y(point.normalizedValue))
      .attr('r', 5.5)
      .attr('fill', item.color)
      .attr('stroke', isDark.value ? '#171717' : '#ffffff')
      .attr('stroke-width', 1.5)
      .attr('opacity', getSeriesOpacity(item.question))
      .on('mouseover', showTooltip)
      .on('mouseout', hideTooltip)
      .on(
        'mousemove',
        function (this: SVGCircleElement, event: MouseEvent, point: SelfReportPoint) {
          moveTooltip(event, point, item.color);
        }
      );
  });

  svg
    .append('g')
    .attr('class', 'x axis')
    .attr('transform', `translate(0, ${height})`)
    .call(d3.axisBottom(x).tickFormat(timeFormat as any))
    .selectAll('text')
    .style('fill', axisColor);

  svg.selectAll('.x.axis path, .x.axis line').style('stroke', axisColor);
}

function showTooltip() {
  d3.select(tooltip.value).style('opacity', '1');
}

function hideTooltip() {
  d3.select(tooltip.value).style('opacity', '0');
}

function moveTooltip(event: MouseEvent, point: SelfReportPoint, color: string) {
  if (!tooltip.value) {
    return;
  }

  const timeFormat = d3.timeFormat('%H:%M');
  d3.select(tooltip.value)
    .style('left', `${event.clientX}px`)
    .style('top', `${event.clientY}px`)
    .style('transform', 'translate(-50%, -120%)');

  const questionElement = document.createElement('div');
  questionElement.style.color = color;
  questionElement.style.fontWeight = '600';
  questionElement.textContent = point.question;
  const ratingElement = document.createElement('div');
  ratingElement.textContent = `Rating: ${point.value} on a scale from 1-${point.scale}${getTooltipScaleSuffix(point)}`;
  const timeElement = document.createElement('div');
  timeElement.textContent = `Self-report taken at: ${timeFormat(point.promptedAt)}`;
  tooltip.value.replaceChildren(questionElement, ratingElement, timeElement);
}

function getTooltipScaleSuffix(point: SelfReportPoint): string {
  if (point.labels.length === 0) {
    return '';
  }
  return `Scale: ${point.labels.join(' / ')}`;
  return ` (${point.labels.join(' / ')})`;
}
</script>

<style scoped>
.self-report-chart {
  position: relative;
  width: 760px;
  max-width: 100%;
}

.self-report-legend {
  display: flex;
  flex-wrap: wrap;
  gap: 0.45rem 1rem;
  margin: 0.25rem 0 0;
  padding-left: 35px;
}

.self-report-legend-item {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  max-width: 350px;
  min-width: 0;
  border: 0;
  padding: 0;
  background: transparent;
  cursor: pointer;
  font-size: 0.75rem;
  color: #4b5563;
  text-align: left;
  transition: opacity 0.15s ease-in-out;
}

.self-report-legend-item-muted {
  opacity: 0.35;
}

.self-report-legend-dot {
  width: 0.55rem;
  height: 0.55rem;
  border-radius: 999px;
  flex: 0 0 auto;
}

.self-report-legend-label {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.self-report-tooltip {
  position: fixed;
  width: max-content;
  max-width: 360px;
  pointer-events: none;
  z-index: 9999;
  text-align: left;
  font-size: 0.75rem;
  line-height: 1.2rem;
}

@media (prefers-color-scheme: dark) {
  .self-report-legend-item {
    color: #d4d4d4;
  }
}
</style>
