<template>
  <div id="tooltip"
      class="bg-white text-gray-700 border border-gray-200 dark:bg-neutral-800 dark:text-neutral-300 dark:border-transparent p-2 rounded opacity-0 transition-opacity ease-in-out duration-300 shadow-lg dark:shadow-neutral-800/80">
    <span id="content"></span>
  </div>
  <svg ref="chart" :width="svgWidth" :height="svgHeight"></svg>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed, watch } from 'vue'
import * as d3 from 'd3'
import { Color } from '../utils/retrospection/types'
import {
  ACTIVITY_LABELS,
  getActivityGroupFromActivityName,
  getBarColorFromDataPoint,
  msToReadableFormat,
  TW_CLASS_ACTIVITY_MAPPINGS
} from '../utils/retrospection/utils'
import { DataPointType, ChartDataPoint } from '../utils/retrospection/types'

const props = defineProps({
  data: {
    type: Array,
    required: true
  },
  type: {
    type: String,
    required: true,
    validator: (value: string) => {
      return ['WINDOW_ACTIVITY'].includes(value)
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
})

const svgWidth = 760
const chart = ref<SVGElement | null>()
const chartSelectedLegendItem = ref<string | null>()
const darkMediaQuery = window.matchMedia('(prefers-color-scheme: dark)')
const isDark = ref(darkMediaQuery.matches)

function onThemeChange(e: MediaQueryListEvent) {
  isDark.value = e.matches
  d3.select(chart.value!).selectAll('*').remove()
  buildChart()
}

const chartStartDate = ref<number>()
const chartEndDate = ref<number>()

onMounted(() => {
  darkMediaQuery.addEventListener('change', onThemeChange)
  const minStartTime = new Date(props.startDate).setHours(0, 0, 0, 0)
  const maxEndTime = new Date(props.endDate).setHours(23, 59, 59, 999)
  if (props.startDate > minStartTime) {
    chartStartDate.value = props.startDate
  } else {
    chartStartDate.value = minStartTime
  }
  if (props.endDate <= maxEndTime) {
    chartEndDate.value = props.endDate
  } else {
    chartEndDate.value = maxEndTime
  }
  buildChart()
})

onUnmounted(() => {
  darkMediaQuery.removeEventListener('change', onThemeChange)
})

const svgHeight = computed(() => {
  if (props.type === 'WINDOW_ACTIVITY') {
    return 135
  } else {
    return 100
  }
})

watch([() => props.data], () => {
  rebuildChartWithAnimation()
})

function getActiveTaskTotalTimeSpentPerActivityGroupArray() {
  const activeTaskTotalTimeSpentPerActivityGroup: {
    activityGroup: string
    totalTime: number
    timeInPercentage: number
  }[] = []
  let totalTimeSpent = 0
  props.data.forEach((dataPoint: any) => {
    const activityGroup = getActivityGroupFromActivityName(dataPoint.activity)
    const totalTime = dataPoint.end - dataPoint.start
    const existingActivityGroup = activeTaskTotalTimeSpentPerActivityGroup.find(
      (item) => item.activityGroup === activityGroup
    )
    if (existingActivityGroup) {
      existingActivityGroup.totalTime += totalTime
    } else {
      activeTaskTotalTimeSpentPerActivityGroup.push({
        activityGroup,
        totalTime,
        timeInPercentage: 0
      })
    }
    totalTimeSpent += totalTime
  })
  activeTaskTotalTimeSpentPerActivityGroup.sort((a, b) => b.totalTime - a.totalTime)
  activeTaskTotalTimeSpentPerActivityGroup.forEach((item) => {
    item.timeInPercentage = (item.totalTime / totalTimeSpent) * 100
  })
  return activeTaskTotalTimeSpentPerActivityGroup
}

function buildChart() {
  const margin = { top: 20, right: 0, bottom: 30, left: 0 }
  const width = svgWidth - margin.left - margin.right
  const height = svgHeight.value - margin.top - margin.bottom

  const barHeight = 35
  const barYOffset = -10

  const svg = d3
    .select(chart.value!)
    .append('svg')
    .attr('width', width + margin.left + margin.right)
    .attr('height', height + margin.top + margin.bottom)
    .append('g')
    .attr('transform', 'translate(' + margin.left + ',' + margin.top + ')')

  svg
    .append('rect')
    .attr('class', 'bar-background')
    .attr('x', 0)
    .attr('width', width)
    .attr('y', barYOffset)
    .attr('height', barHeight)
    .attr('fill', isDark.value ? (Color as any)['neutral-800'] : '#e5e7eb')
    .style('opacity', 1)
    .attr('rx', 8)

  const x = d3.scaleTime().domain([chartStartDate.value!, chartEndDate.value!]).range([0, width])

  const timeFormat = d3.timeFormat('%H:%M')

  svg
    .selectAll('.bar')
    .data(props.data)
    .enter()
    .append('rect')
    .attr('class', (d: any) => {
      return `bar bar-${getActivityGroupFromActivityName(d.activity)}`
    })
    .attr('x', (d: any) => x(d.start))
    .attr('width', (d: any) => x(d.end) - x(d.start))
    .attr('y', barYOffset)
    .attr('height', barHeight)
    .style('fill', (d: any) => {
      if (d.type === DataPointType.USER_COMPUTER_ACTIVITY) {
        return (Color as any)['neutral-400']
      }
      return getBarColorFromDataPoint(d.color)
    })
    .style('opacity', 1)
    .on('mouseover', function () {
      d3.select('#tooltip').style('opacity', null)
    })
    .on('mouseout', function () {
      d3.select('#tooltip').style('opacity', '0')
    })
    .on('mousemove', function (this: SVGRectElement, _event: any, d: unknown) {
      const dataPoint = d as ChartDataPoint
      const durationInMinutes = msToReadableFormat(dataPoint.end.getTime() - dataPoint.start.getTime(), false, false)

      const barBoundingRect = this.getBoundingClientRect()
      const xPosition = barBoundingRect.x + barBoundingRect.width / 2
      const yPosition = barBoundingRect.y - barBoundingRect.height + 10

      const tooltipContent = `<div class="text-${dataPoint.color}">${ACTIVITY_LABELS[getActivityGroupFromActivityName(dataPoint.activity)]}</div> ${timeFormat(dataPoint.start)} - ${timeFormat(dataPoint.end)} (${durationInMinutes})`

      d3.select('#tooltip')
        .style('left', xPosition + 'px')
        .style('top', yPosition + 'px')
        .style('opacity', '1')
        .style('transform', `translate(-50%, -${barBoundingRect.height}px)`)
        .select('#content')
        .html(tooltipContent)
    })

  // Add legend for window activity
  if (props.type === 'WINDOW_ACTIVITY') {
    drawLegend(getLegendDataForWindowActivity(), true)
  }

  const axisColor = isDark.value ? '#a3a3a3' : '#374151'
  svg
    .append('g')
    .attr('class', 'x axis')
    .attr('transform', `translate(0, ${barYOffset + barHeight})`)
    .call(d3.axisBottom(x).tickFormat(timeFormat as any))
    .selectAll('text')
    .style('fill', axisColor)

  svg.selectAll('.x.axis path, .x.axis line').style('stroke', axisColor)
}

interface LegendDataPoint {
  text: string
  color: string
  key: string
}

function getLegendDataForWindowActivity(): LegendDataPoint[] {
  const legendData: LegendDataPoint[] = []
  const activeTaskTotalTimeSpentPerActivityGroup =
    getActiveTaskTotalTimeSpentPerActivityGroupArray()
  activeTaskTotalTimeSpentPerActivityGroup.forEach((item) => {
    legendData.push({
      text: `${ACTIVITY_LABELS[item.activityGroup] || 'Other'} (${msToReadableFormat(item.totalTime, false, false)})`,
      color: (Color as any)[TW_CLASS_ACTIVITY_MAPPINGS[item.activityGroup]],
      key: item.activityGroup
    })
  })
  return legendData
}

function drawLegend(legendData: LegendDataPoint[], enableClickableLegend = false) {
  // Remove existing legend elements
  d3.select(chart.value!).select('.legend').remove()
  d3.select(chart.value!).append('g').attr('class', 'legend')

  const legendStartPositionY = 85
  const legendStartPositionX = 35
  const dotRadius = 5
  const lineHeight = 22

  let totalWidth = legendStartPositionX
  const legendPositions: number[] = []
  const startNewLinesAtItemIndex: number[] = []
  let currentLegendItemLine = 0
  const legendLabels = d3.select(chart.value!).select('.legend').selectAll('.legend-label')
  legendLabels
    .data(legendData)
    .enter()
    .append('text')
    .attr('class', (d: LegendDataPoint) => `legend-label legend-label-${d.key}`)
    .style('fill', function (d: LegendDataPoint) {
      return d.color
    })
    .text(function (d: LegendDataPoint) {
      return d.text
    })
    .style('user-select', 'none')
    .attr('font-size', '12px')
    .attr('text-anchor', 'left')
    .style('alignment-baseline', 'middle')
    .style('opacity', 1)
    .attr('x', function (this: SVGTextElement, _d: LegendDataPoint, i: number) {
      const current = d3.select(this)
      const currentNodeWidth = current.node()!.getBBox().width
      let previousWidth = totalWidth
      if (previousWidth + currentNodeWidth + 35 > svgWidth) {
        startNewLinesAtItemIndex.push(i)
        totalWidth = legendStartPositionX
        previousWidth = legendStartPositionX
      }
      legendPositions.push(totalWidth)
      totalWidth += currentNodeWidth + 35
      return previousWidth
    })
    .attr('y', function (_d: LegendDataPoint, i: number) {
      if (startNewLinesAtItemIndex.includes(i)) {
        currentLegendItemLine++
      }
      return legendStartPositionY + lineHeight * currentLegendItemLine + 1
    })

  if (enableClickableLegend) {
    d3.select(chart.value!)
      .select('.legend')
      .selectAll('.legend-label')
      .attr('cursor', 'pointer')
      .on('click', function (_e: any, d: any) {
        d3.select(chart.value!).selectAll(`.legend-label`).style('opacity', null)
        d3.select(chart.value!).selectAll(`.legend-dot`).style('opacity', null)
        d3.select(chart.value!).selectAll(`.bar`).style('opacity', null)
        if (chartSelectedLegendItem.value === d.key) {
          chartSelectedLegendItem.value = null
          return
        } else {
          chartSelectedLegendItem.value = d.key
          d3.select(chart.value!)
            .selectAll(`.legend-label:not(.legend-label-${d.key})`)
            .style('opacity', 0.3)
          d3.select(chart.value!)
            .selectAll(`.legend-dot:not(.legend-dot-${d.key})`)
            .style('opacity', 0.3)
          d3.select(chart.value!)
            .selectAll(`.bar:not(.bar-${d.key})`)
            .style('opacity', 0.15)
        }
      })
  }

  currentLegendItemLine = 0
  d3.select(chart.value!)
    .select('.legend')
    .selectAll('.legend-dot')
    .data(legendData)
    .enter()
    .append('circle')
    .attr('class', (d: LegendDataPoint) => `legend-dot legend-dot-${d.key}`)
    .attr('cx', function (_d: LegendDataPoint, i: number) {
      return legendPositions[i] - 10
    })
    .attr('cy', function (_d: LegendDataPoint, i: number) {
      if (startNewLinesAtItemIndex.includes(i)) {
        currentLegendItemLine++
      }
      return legendStartPositionY + lineHeight * currentLegendItemLine
    })
    .attr('r', dotRadius)
    .style('fill', function (d: LegendDataPoint) {
      return d.color
    })
    .style('opacity', 1)
}

function rebuildChartWithAnimation() {
  chartSelectedLegendItem.value = null
  const margin = { top: 20, right: 20, bottom: 40, left: 20 }
  const width = svgWidth - margin.left - margin.right
  const svg = d3.select(chart.value!).select('svg')
  const x = d3.scaleTime().domain([chartStartDate.value!, chartEndDate.value!]).range([0, width])

  const bars = svg.selectAll('.bar').data(props.data)

  bars
    .transition()
    .duration(300)
    .attr('x', (d: any) => x(d.start))
    .attr('width', (d: any) => x(d.end) - x(d.start))
    .style('opacity', 1)

  bars.exit().transition().duration(300).attr('width', 0).remove()

  // Update legend
  if (props.type === 'WINDOW_ACTIVITY') {
    drawLegend(getLegendDataForWindowActivity(), true)
  }
}
</script>

<style scoped>
#tooltip {
  position: absolute;
  text-align: center;
  width: auto;
  height: auto;
  padding: 5px 10px;
  border-radius: 10px;
  pointer-events: none;
  z-index: 9999;
}
</style>
