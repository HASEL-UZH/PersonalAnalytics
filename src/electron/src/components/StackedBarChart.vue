<template>
  <div ref="chartContainer" class="stacked-bar-chart">
    <div
      id="tooltip"
      class="rounded border border-gray-200 bg-white p-2 text-gray-700 opacity-0 shadow-lg transition-opacity duration-300 ease-in-out dark:border-transparent dark:bg-neutral-800 dark:text-neutral-300 dark:shadow-neutral-800/80"
    >
      <span id="content"></span>
    </div>
    <svg ref="chart" :width="svgWidth" :height="svgHeight"></svg>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch, type PropType } from 'vue';
import * as d3 from 'd3';
import { Color } from '../utils/retrospection/types';
import {
  ACTIVITY_LABELS,
  escapeHtml,
  formatDuration,
  getActivityGroupFromActivityName,
  getBarColorFromDataPoint,
  TW_CLASS_ACTIVITY_MAPPINGS
} from '../utils/retrospection/utils';
import { positionTooltipWithinViewport } from '../utils/tooltipPosition';
import {
  DataPointType,
  ChartDataPoint,
  type TimelineHoverDetail
} from '../utils/retrospection/types';

const props = defineProps({
  data: {
    type: Array as PropType<ChartDataPoint[]>,
    required: true
  },
  type: {
    type: String,
    required: true,
    validator: (value: string) => {
      return ['WINDOW_ACTIVITY'].includes(value);
    }
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

const svgWidth = ref(760);
const svgHeight = ref(135);
const chartContainer = ref<HTMLElement | null>(null);
const chart = ref<SVGElement | null>();
const chartSelectedLegendItem = ref<string | null>();
const darkMediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
const isDark = ref(darkMediaQuery.matches);
let resizeObserver: ResizeObserver | undefined;

function onThemeChange(e: MediaQueryListEvent) {
  isDark.value = e.matches;
  rebuildChart();
}

onMounted(() => {
  darkMediaQuery.addEventListener('change', onThemeChange);
  updateChartWidth();
  resizeObserver = new ResizeObserver(() => {
    const previousWidth = svgWidth.value;
    updateChartWidth();
    if (svgWidth.value !== previousWidth) {
      rebuildChart();
    }
  });
  if (chartContainer.value) {
    resizeObserver.observe(chartContainer.value);
  }
  rebuildChart();
});

onUnmounted(() => {
  darkMediaQuery.removeEventListener('change', onThemeChange);
  resizeObserver?.disconnect();
});

watch([() => props.data, () => props.startDate, () => props.endDate], rebuildChart);

function rebuildChart() {
  if (!chart.value) {
    return;
  }
  chartSelectedLegendItem.value = null;
  svgHeight.value = props.type === 'WINDOW_ACTIVITY' ? 135 : 100;
  d3.select('#tooltip').style('opacity', '0');
  d3.select(chart.value).selectAll('*').remove();
  buildChart();
}

function updateChartWidth(): void {
  const width = Math.floor(chartContainer.value?.getBoundingClientRect().width ?? 0);
  if (width > 0) {
    svgWidth.value = width;
  }
}

function getActiveTaskTotalTimeSpentPerActivityGroupArray() {
  const activeTaskTotalTimeSpentPerActivityGroup: {
    activityGroup: string;
    totalTime: number;
    timeInPercentage: number;
  }[] = [];
  let totalTimeSpent = 0;
  props.data.forEach((dataPoint) => {
    const activityGroup = getActivityGroupFromActivityName(dataPoint.activity);
    const totalTime = dataPoint.end.getTime() - dataPoint.start.getTime();
    const existingActivityGroup = activeTaskTotalTimeSpentPerActivityGroup.find(
      (item) => item.activityGroup === activityGroup
    );
    if (existingActivityGroup) {
      existingActivityGroup.totalTime += totalTime;
    } else {
      activeTaskTotalTimeSpentPerActivityGroup.push({
        activityGroup,
        totalTime,
        timeInPercentage: 0
      });
    }
    totalTimeSpent += totalTime;
  });
  activeTaskTotalTimeSpentPerActivityGroup.sort((a, b) => b.totalTime - a.totalTime);
  activeTaskTotalTimeSpentPerActivityGroup.forEach((item) => {
    item.timeInPercentage = (item.totalTime / totalTimeSpent) * 100;
  });
  return activeTaskTotalTimeSpentPerActivityGroup;
}

function shouldShowAppName(detail: TimelineHoverDetail): boolean {
  if (!detail.appName) {
    return false;
  }
  return detail.appName.trim().toLowerCase() !== detail.title.trim().toLowerCase();
}

function getDetailInitial(detail: TimelineHoverDetail): string {
  return (detail.appName || detail.title).trim().charAt(0).toUpperCase() || '?';
}

function renderTooltipDetailIcon(detail: TimelineHoverDetail): string {
  if (detail.iconDataUrl) {
    return `<img src="${escapeHtml(detail.iconDataUrl)}" alt="" style="width: 16px; height: 16px; border-radius: 4px; flex: 0 0 auto;" />`;
  }

  return `
    <span style="width: 16px; height: 16px; border-radius: 4px; flex: 0 0 auto; display: inline-flex; align-items: center; justify-content: center; background: rgba(148, 163, 184, 0.22); color: #64748b; font-size: 10px; font-weight: 700;">
      ${escapeHtml(getDetailInitial(detail))}
    </span>
  `;
}

function renderTooltipDetail(detail: TimelineHoverDetail): string {
  const duration = formatDuration(detail.durationMs);
  const title = escapeHtml(detail.title);
  const tooltipTitle = escapeHtml(detail.tooltipTitle || detail.title);
  const appName = detail.appName ? escapeHtml(detail.appName) : '';
  const appLabel = shouldShowAppName(detail)
    ? `<span style="color: #64748b;">${appName}</span>`
    : '';

  return `
    <li title="${tooltipTitle}" style="display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: 10px; align-items: center;">
      <span style="min-width: 0; display: inline-flex; align-items: center; gap: 6px;">
        ${renderTooltipDetailIcon(detail)}
        <span style="min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">
          ${title}${appLabel ? `<span style="color: #94a3b8;"> &middot; </span>${appLabel}` : ''}
        </span>
      </span>
      <span style="color: #64748b; white-space: nowrap;">${duration}</span>
    </li>
  `;
}

function renderTooltipDetails(dataPoint: ChartDataPoint): string {
  const details = dataPoint.details?.filter((detail) => detail.durationMs >= 60000) || [];
  if (!details.length) {
    return '';
  }

  const detailRows = details.map(renderTooltipDetail).join('');
  const hiddenDetailCount = dataPoint.hiddenDetailCount || 0;
  const hiddenRow =
    hiddenDetailCount > 0 ? `<li style="color: #64748b;">+ ${hiddenDetailCount} more</li>` : '';

  return `
    <ol style="margin: 8px 0 0; padding: 8px 0 0; border-top: 1px solid rgba(148, 163, 184, 0.35); list-style: none; text-align: left;">
      ${detailRows}
      ${hiddenRow}
    </ol>
  `;
}

function positionTooltip(event: MouseEvent, barBoundingRect: DOMRect) {
  const tooltipNode = document.querySelector<HTMLDivElement>('#tooltip');
  if (!tooltipNode) {
    return;
  }

  positionTooltipWithinViewport(tooltipNode, {
    x: event.clientX,
    top: barBoundingRect.top,
    bottom: barBoundingRect.bottom
  });
}

function buildChart() {
  const margin = { top: 20, right: 0, bottom: 30, left: 0 };
  const width = svgWidth.value - margin.left - margin.right;

  const barHeight = 35;
  const barYOffset = -10;

  const svg = d3
    .select(chart.value!)
    .attr('width', svgWidth.value)
    .attr('height', svgHeight.value)
    .append('g')
    .attr('transform', 'translate(' + margin.left + ',' + margin.top + ')');

  svg
    .append('rect')
    .attr('class', 'bar-background')
    .attr('x', 0)
    .attr('width', width)
    .attr('y', barYOffset)
    .attr('height', barHeight)
    .attr('fill', isDark.value ? Color['neutral-800'] : '#e5e7eb')
    .style('opacity', 1)
    .attr('rx', 8);

  const x = d3.scaleTime().domain([props.startDate, props.endDate]).range([0, width]);

  const timeFormat = d3.timeFormat('%H:%M');
  const formatTimeTick = (value: Date | d3.NumberValue) =>
    timeFormat(value instanceof Date ? value : new Date(Number(value)));

  svg
    .selectAll<SVGRectElement, ChartDataPoint>('.bar')
    .data(props.data)
    .enter()
    .append('rect')
    .attr('class', (d) => {
      return `bar bar-${getActivityGroupFromActivityName(d.activity)}`;
    })
    .attr('x', (d) => x(d.start))
    .attr('width', (d) => x(d.end) - x(d.start))
    .attr('y', barYOffset)
    .attr('height', barHeight)
    .style('fill', (d) => {
      if (d.type === DataPointType.USER_COMPUTER_ACTIVITY) {
        return Color['neutral-400'];
      }
      return getBarColorFromDataPoint(d.color);
    })
    .style('opacity', 1)
    .on('mouseover', function () {
      d3.select('#tooltip').style('opacity', null);
    })
    .on('mouseout', function () {
      d3.select('#tooltip').style('opacity', '0');
    })
    .on('mousemove', function (this: SVGRectElement, event: MouseEvent, d: unknown) {
      const dataPoint = d as ChartDataPoint;
      const duration = formatDuration(dataPoint.end.getTime() - dataPoint.start.getTime());

      const barBoundingRect = this.getBoundingClientRect();

      const tooltipContent = `
        <div style="text-align: center;">
          <span class="text-${dataPoint.color}" style="font-weight: 600;">${ACTIVITY_LABELS[getActivityGroupFromActivityName(dataPoint.activity)]}</span>
          <span>${timeFormat(dataPoint.start)} - ${timeFormat(dataPoint.end)} (${duration})</span>
        </div>
        ${renderTooltipDetails(dataPoint)}
      `;

      const tooltip = d3.select('#tooltip');
      tooltip.style('opacity', '1').select('#content').html(tooltipContent);
      positionTooltip(event, barBoundingRect);
    });

  // Add legend for window activity
  if (props.type === 'WINDOW_ACTIVITY') {
    drawLegend(getLegendDataForWindowActivity(), true);
  }

  const axisColor = isDark.value ? '#a3a3a3' : '#374151';
  const axisLabels = svg
    .append('g')
    .attr('class', 'x axis')
    .attr('transform', `translate(0, ${barYOffset + barHeight})`)
    .call(d3.axisBottom(x).tickFormat(formatTimeTick))
    .selectAll('text')
    .style('fill', axisColor);

  const labels = axisLabels.nodes();
  labels.forEach((label, index) => {
    if (index === 0) {
      d3.select(label).attr('text-anchor', 'start').attr('x', 4);
    } else if (index === labels.length - 1) {
      d3.select(label).attr('text-anchor', 'end').attr('x', -4);
    }
  });

  svg.selectAll('.x.axis path, .x.axis line').style('stroke', axisColor);
}

interface LegendDataPoint {
  text: string;
  color: string;
  key: string;
}

function getLegendDataForWindowActivity(): LegendDataPoint[] {
  const legendData: LegendDataPoint[] = [];
  const activeTaskTotalTimeSpentPerActivityGroup =
    getActiveTaskTotalTimeSpentPerActivityGroupArray();
  activeTaskTotalTimeSpentPerActivityGroup.forEach((item) => {
    const colorKey = TW_CLASS_ACTIVITY_MAPPINGS[item.activityGroup] as keyof typeof Color;
    legendData.push({
      text: `${ACTIVITY_LABELS[item.activityGroup] || 'Other'} (${formatDuration(item.totalTime)})`,
      color: Color[colorKey],
      key: item.activityGroup
    });
  });
  return legendData;
}

function drawLegend(legendData: LegendDataPoint[], enableClickableLegend = false) {
  // Remove existing legend elements
  d3.select(chart.value!).select('.legend').remove();
  d3.select(chart.value!).append('g').attr('class', 'legend');

  const legendStartPositionY = 85;
  const legendStartPositionX = 35;
  const dotRadius = 5;
  const lineHeight = 22;

  let totalWidth = legendStartPositionX;
  const legendPositions: number[] = [];
  const startNewLinesAtItemIndex: number[] = [];
  let currentLegendItemLine = 0;
  const legendLabels = d3.select(chart.value!).select('.legend').selectAll('.legend-label');
  legendLabels
    .data(legendData)
    .enter()
    .append('text')
    .attr('class', (d: LegendDataPoint) => `legend-label legend-label-${d.key}`)
    .style('fill', function (d: LegendDataPoint) {
      return d.color;
    })
    .text(function (d: LegendDataPoint) {
      return d.text;
    })
    .style('user-select', 'none')
    .attr('font-size', '12px')
    .attr('text-anchor', 'left')
    .style('alignment-baseline', 'middle')
    .style('opacity', 1)
    .attr('x', function (this: SVGTextElement, _d: LegendDataPoint, i: number) {
      const current = d3.select(this);
      const currentNodeWidth = current.node()!.getBBox().width;
      let previousWidth = totalWidth;
      if (previousWidth + currentNodeWidth + 35 > svgWidth.value) {
        startNewLinesAtItemIndex.push(i);
        totalWidth = legendStartPositionX;
        previousWidth = legendStartPositionX;
      }
      legendPositions.push(totalWidth);
      totalWidth += currentNodeWidth + 35;
      return previousWidth;
    })
    .attr('y', function (_d: LegendDataPoint, i: number) {
      if (startNewLinesAtItemIndex.includes(i)) {
        currentLegendItemLine++;
      }
      return legendStartPositionY + lineHeight * currentLegendItemLine + 1;
    });

  if (enableClickableLegend) {
    d3.select(chart.value!)
      .select('.legend')
      .selectAll<SVGTextElement, LegendDataPoint>('.legend-label')
      .attr('cursor', 'pointer')
      .on('click', function (_event: MouseEvent, d: LegendDataPoint) {
        d3.select(chart.value!).selectAll(`.legend-label`).style('opacity', null);
        d3.select(chart.value!).selectAll(`.legend-dot`).style('opacity', null);
        d3.select(chart.value!).selectAll(`.bar`).style('opacity', null);
        if (chartSelectedLegendItem.value === d.key) {
          chartSelectedLegendItem.value = null;
          return;
        } else {
          chartSelectedLegendItem.value = d.key;
          d3.select(chart.value!)
            .selectAll(`.legend-label:not(.legend-label-${d.key})`)
            .style('opacity', 0.3);
          d3.select(chart.value!)
            .selectAll(`.legend-dot:not(.legend-dot-${d.key})`)
            .style('opacity', 0.3);
          d3.select(chart.value!).selectAll(`.bar:not(.bar-${d.key})`).style('opacity', 0.15);
        }
      });
  }

  currentLegendItemLine = 0;
  d3.select(chart.value!)
    .select('.legend')
    .selectAll('.legend-dot')
    .data(legendData)
    .enter()
    .append('circle')
    .attr('class', (d: LegendDataPoint) => `legend-dot legend-dot-${d.key}`)
    .attr('cx', function (_d: LegendDataPoint, i: number) {
      return legendPositions[i] - 10;
    })
    .attr('cy', function (_d: LegendDataPoint, i: number) {
      if (startNewLinesAtItemIndex.includes(i)) {
        currentLegendItemLine++;
      }
      return legendStartPositionY + lineHeight * currentLegendItemLine;
    })
    .attr('r', dotRadius)
    .style('fill', function (d: LegendDataPoint) {
      return d.color;
    })
    .style('opacity', 1);

  svgHeight.value = Math.max(
    svgHeight.value,
    legendStartPositionY + lineHeight * currentLegendItemLine + 18
  );
}
</script>

<style scoped>
.stacked-bar-chart {
  position: relative;
  width: 100%;
  min-width: 0;
}

.stacked-bar-chart svg {
  display: block;
  max-width: 100%;
}

#tooltip {
  position: fixed;
  font-size: 12px;
  line-height: 1.35;
  text-align: left;
  width: auto;
  max-width: min(360px, calc(100vw - 16px));
  height: auto;
  padding: 5px 10px;
  border-radius: 10px;
  pointer-events: none;
  z-index: 9999;
}
</style>
