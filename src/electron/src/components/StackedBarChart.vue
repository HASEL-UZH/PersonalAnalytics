<template>
  <div
    id="tooltip"
    class="rounded border border-gray-200 bg-white p-2 text-gray-700 opacity-0 shadow-lg transition-opacity duration-300 ease-in-out dark:border-transparent dark:bg-neutral-800 dark:text-neutral-300 dark:shadow-neutral-800/80"
  >
    <span id="content"></span>
  </div>
  <svg ref="chart" :width="svgWidth" :height="svgHeight"></svg>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed, watch } from 'vue';
import * as d3 from 'd3';
import { Color } from '../utils/retrospection/types';
import {
  ACTIVITY_LABELS,
  getActivityGroupFromActivityName,
  getBarColorFromDataPoint,
  msToReadableFormat,
  TW_CLASS_ACTIVITY_MAPPINGS
} from '../utils/retrospection/utils';
import {
  DataPointType,
  ChartDataPoint,
  type TimelineHoverDetail
} from '../utils/retrospection/types';

const props = defineProps({
  data: {
    type: Array,
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

const svgWidth = 760;
const chart = ref<SVGElement | null>();
const chartSelectedLegendItem = ref<string | null>();
const darkMediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
const isDark = ref(darkMediaQuery.matches);

function onThemeChange(e: MediaQueryListEvent) {
  isDark.value = e.matches;
  d3.select(chart.value!).selectAll('*').remove();
  buildChart();
}

const chartStartDate = ref<number>();
const chartEndDate = ref<number>();

onMounted(() => {
  darkMediaQuery.addEventListener('change', onThemeChange);
  const minStartTime = new Date(props.startDate).setHours(0, 0, 0, 0);
  const maxEndTime = new Date(props.endDate).setHours(23, 59, 59, 999);
  if (props.startDate > minStartTime) {
    chartStartDate.value = props.startDate;
  } else {
    chartStartDate.value = minStartTime;
  }
  if (props.endDate <= maxEndTime) {
    chartEndDate.value = props.endDate;
  } else {
    chartEndDate.value = maxEndTime;
  }
  buildChart();
});

onUnmounted(() => {
  darkMediaQuery.removeEventListener('change', onThemeChange);
});

const svgHeight = computed(() => {
  if (props.type === 'WINDOW_ACTIVITY') {
    return 135;
  } else {
    return 100;
  }
});

watch([() => props.data], () => {
  rebuildChartWithAnimation();
});

function getActiveTaskTotalTimeSpentPerActivityGroupArray() {
  const activeTaskTotalTimeSpentPerActivityGroup: {
    activityGroup: string;
    totalTime: number;
    timeInPercentage: number;
  }[] = [];
  let totalTimeSpent = 0;
  props.data.forEach((dataPoint: any) => {
    const activityGroup = getActivityGroupFromActivityName(dataPoint.activity);
    const totalTime = dataPoint.end - dataPoint.start;
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

function escapeHtml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
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

function renderTooltipDuration(durationInMs: number): string {
  const totalMinutes = Math.round(durationInMs / 60000);
  if (totalMinutes < 1) {
    return '< 1 min';
  }
  if (totalMinutes < 60) {
    return `${totalMinutes} min`;
  }

  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  return minutes ? `${hours} h ${minutes} min` : `${hours} h`;
}

function renderTooltipDetail(detail: TimelineHoverDetail): string {
  const duration = renderTooltipDuration(detail.durationMs);
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

function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max);
}

function positionTooltip(event: MouseEvent, barBoundingRect: DOMRect) {
  const viewportPadding = 8;
  const tooltipGap = 10;
  const tooltip = d3.select<HTMLDivElement, unknown>('#tooltip');
  const tooltipNode = tooltip.node();
  if (!tooltipNode) {
    return;
  }

  const tooltipRect = tooltipNode.getBoundingClientRect();
  const maxLeft = Math.max(
    viewportPadding,
    window.innerWidth - tooltipRect.width - viewportPadding
  );
  const left = clamp(event.clientX - tooltipRect.width / 2, viewportPadding, maxLeft);

  const topAboveBar = barBoundingRect.top - tooltipRect.height - tooltipGap;
  const topBelowBar = barBoundingRect.bottom + tooltipGap;
  const maxTop = Math.max(
    viewportPadding,
    window.innerHeight - tooltipRect.height - viewportPadding
  );
  const top =
    topAboveBar >= viewportPadding ? topAboveBar : clamp(topBelowBar, viewportPadding, maxTop);

  tooltip
    .style('left', `${left + window.scrollX}px`)
    .style('top', `${top + window.scrollY}px`)
    .style('transform', 'none');
}

function buildChart() {
  const margin = { top: 20, right: 0, bottom: 30, left: 0 };
  const width = svgWidth - margin.left - margin.right;
  const height = svgHeight.value - margin.top - margin.bottom;

  const barHeight = 35;
  const barYOffset = -10;

  const svg = d3
    .select(chart.value!)
    .append('svg')
    .attr('width', width + margin.left + margin.right)
    .attr('height', height + margin.top + margin.bottom)
    .append('g')
    .attr('transform', 'translate(' + margin.left + ',' + margin.top + ')');

  svg
    .append('rect')
    .attr('class', 'bar-background')
    .attr('x', 0)
    .attr('width', width)
    .attr('y', barYOffset)
    .attr('height', barHeight)
    .attr('fill', isDark.value ? (Color as any)['neutral-800'] : '#e5e7eb')
    .style('opacity', 1)
    .attr('rx', 8);

  const x = d3.scaleTime().domain([chartStartDate.value!, chartEndDate.value!]).range([0, width]);

  const timeFormat = d3.timeFormat('%H:%M');

  svg
    .selectAll('.bar')
    .data(props.data)
    .enter()
    .append('rect')
    .attr('class', (d: any) => {
      return `bar bar-${getActivityGroupFromActivityName(d.activity)}`;
    })
    .attr('x', (d: any) => x(d.start))
    .attr('width', (d: any) => x(d.end) - x(d.start))
    .attr('y', barYOffset)
    .attr('height', barHeight)
    .style('fill', (d: any) => {
      if (d.type === DataPointType.USER_COMPUTER_ACTIVITY) {
        return (Color as any)['neutral-400'];
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
      const duration = renderTooltipDuration(dataPoint.end.getTime() - dataPoint.start.getTime());

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
  svg
    .append('g')
    .attr('class', 'x axis')
    .attr('transform', `translate(0, ${barYOffset + barHeight})`)
    .call(d3.axisBottom(x).tickFormat(timeFormat as any))
    .selectAll('text')
    .style('fill', axisColor);

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
    legendData.push({
      text: `${ACTIVITY_LABELS[item.activityGroup] || 'Other'} (${msToReadableFormat(item.totalTime, false, false)})`,
      color: (Color as any)[TW_CLASS_ACTIVITY_MAPPINGS[item.activityGroup]],
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
      if (previousWidth + currentNodeWidth + 35 > svgWidth) {
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
      .selectAll('.legend-label')
      .attr('cursor', 'pointer')
      .on('click', function (_e: any, d: any) {
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
}

function rebuildChartWithAnimation() {
  chartSelectedLegendItem.value = null;
  const margin = { top: 20, right: 20, bottom: 40, left: 20 };
  const width = svgWidth - margin.left - margin.right;
  const svg = d3.select(chart.value!).select('svg');
  const x = d3.scaleTime().domain([chartStartDate.value!, chartEndDate.value!]).range([0, width]);

  const bars = svg.selectAll('.bar').data(props.data);

  bars
    .transition()
    .duration(300)
    .attr('x', (d: any) => x(d.start))
    .attr('width', (d: any) => x(d.end) - x(d.start))
    .style('opacity', 1);

  bars.exit().transition().duration(300).attr('width', 0).remove();

  // Update legend
  if (props.type === 'WINDOW_ACTIVITY') {
    drawLegend(getLegendDataForWindowActivity(), true);
  }
}
</script>

<style scoped>
#tooltip {
  position: absolute;
  font-size: 12px;
  line-height: 1.35;
  text-align: left;
  width: auto;
  max-width: 360px;
  height: auto;
  padding: 5px 10px;
  border-radius: 10px;
  pointer-events: none;
  z-index: 9999;
}
</style>
