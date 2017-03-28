// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-27
// 
// Licensed under the MIT License.

using Shared;
using System;
using GoalSetting.Rules;
using Shared.Data;
using GoalSetting.Data;
using GoalSetting.Model;
using System.Collections.Generic;
using System.Linq;

namespace GoalSetting.Visualizers
{
    public class DayVisualization : BaseVisualization, IVisualization
    {
        private PARule _rule;
        private DateTimeOffset _date;

        public DayVisualization(DateTimeOffset date, PARule rule)
        {
            Title = rule.ToString();
            this._rule = rule;
            this._date = date;
            IsEnabled = true;
            Size = VisSize.Wide;
            Order = 0;
        }

        public override string GetHtml()
        {
            var html = string.Empty;
            var startOfWork = Database.GetInstance().GetUserWorkStart(DateTime.Now.Date);
            var endOfWork = DateTime.Now;

            Console.WriteLine("Draw visualization from: " + startOfWork + " to " + endOfWork);
            var activities = DatabaseConnector.GetActivitiesSinceAndBefore(startOfWork, endOfWork);
            activities = DataHelper.MergeSameActivities(activities, Settings.MinimumSwitchTime);

            var targetActivity = _rule.Activity;

            // CSS
            html += "<style type='text/css'>";
            html += ".c3-line { stroke-width: 2px; }";
            html += ".c3-grid text, c3.grid line { fill: black; }";
            html += ".axis path, .axis line {fill: none; stroke: black; stroke-width: 1; shape-rendering: crispEdges;}";
            html += "</style>";
            
            //HTML
            html += "<div id='chart' style='align: center'></div>";
            html += "<d<p style='text-align: center; font-size: 0.66em;'>Number of switches per time</p>";

            //JS
            html += "<script>";

            html += "var actualHeight = document.getElementsByClassName('item Wide')[0].offsetHeight;";
            html += "var actualWidth = document.getElementsByClassName('item Wide')[0].offsetWidth;";
            html += "var margin = {top: 30, right: 30, bottom: 30, left: 40}, width = (actualWidth * 0.97)- margin.left - margin.right, height = (actualHeight * 0.73) - margin.top - margin.bottom;";

            //Prepare data
            html += "var parseTime = d3.time.format('%H:%M').parse;";
            html += GenerateJSData(GenerateData(activities, targetActivity));
            html += "data.forEach(function(d) {d.start = parseTime(d.start); d.end = parseTime(d.end);});";
            
            //Prepare scales
            html += "var x = d3.time.scale().range([0, width]);";
            html += "var y0 = d3.scale.linear().range([height, 0]);";

            //Prepare axis
            html += "var xAxis = d3.svg.axis().scale(x).orient('bottom').tickFormat(d3.time.format('%H:%M'));";
            html += "var yAxisLeft = d3.svg.axis().scale(y0).orient('left').ticks(5);";

            //Prepare lines
            html += "var limit = " + _rule.Rule.TargetValue + ";";
            html += "var valueLine1 = d3.svg.line().interpolate('step-after').defined(function(d) {return d.switch != null; }).x(function(d) {return x(d.start); }).y(function(d) { return y0(d.switch); });";
            
            //Prepare chart area
            html += "var svg = d3.select('#chart').append('svg').attr('width', width + margin.left + margin.right).attr('height', height + margin.top + margin.bottom).append('g').attr('transform', 'translate(' + margin.left + ',' + margin.top + ')');";

            //Prepare patterns to color rectangles
            html += @"var pattern = svg.append('defs')
                    .append('pattern')
                    .attr({ id: 'success-pattern', width: '8', height: '8', patternUnits: 'userSpaceOnUse', patternTransform: 'rotate(60)'})
	                .append('rect')
                    .attr({ width: '4', height: '8', transform: 'translate(0,0)', opacity: '0.25', fill: '" + Shared.Settings.RetrospectionColorHex + "' });";

            html += @"var pattern = svg.append('defs')
                    .append('pattern')
                    .attr({ id: 'error-pattern', width: '8', height: '8', patternUnits: 'userSpaceOnUse', patternTransform: 'rotate(60)'})
	                .append('rect')
                    .attr({ width: '4', height: '8', transform: 'translate(0,0)', opacity: '0.25', fill: 'red' });";

            //Prepare domain of axes
            html += "x.domain( [d3.min(data, function(d) { return d.start; }), d3.max(data, function(d) { return d.end; }) ] );";
            html += "var switchValues = data.map(function(o){return o.switch;}).filter(function(val) {return val !== null});";
            html += "var timeValues = data.map(function(o){return o.time;}).filter(function(val) {return val !== null});";
            html += "y0.domain([d3.min(switchValues) * 0.95, d3.max(data, function(d) {return Math.max(d.switch);}) * 1.01]);";
         
            //Draw lines and axes
            html += "svg.append('path').style('stroke', '" + Shared.Settings.RetrospectionColorHex + "').attr('d', valueLine1(data.filter(function(d) {return d.switch <= limit;}))).attr('fill', 'none').attr('stroke-width', '3');";
            html += "svg.append('path').style('stroke', 'red').attr('d', valueLine1(data.filter(function(d) {return d.switch >= limit;}))).attr('fill', 'none').attr('stroke-width', '3');";
            html += "xAxisYPosition = height;";
            html += "svg.append('g').attr('class', 'x axis').attr('transform', 'translate(0,' + xAxisYPosition + ')').call(xAxis);";
            html += "svg.append('g').attr('class', 'y axis').style('fill', 'black').call(yAxisLeft);";

            //Draw legend
            html += "svg.append('text').attr('x', 0).attr('y', -10).style('text-anchor', 'middle').text('# Switches');";

            //Draw hatched rectangles
            html += @"svg.append('g')
                    .attr('id', 'bars')
                    .selectAll('rect')
                    .data(data.filter(function(d){return d.switch <= limit;}))
                    .enter()
                    .append('rect')
                    .attr({'x':function(d) {return x(d.start);},'y':function(d){ return y0(d.switch); } })
					.style('fill', 'url(#success-pattern)')
                    .attr('height', function(d) {return xAxisYPosition - y0(d.switch);})
					.attr('width', function(d){ return x(d.end) - x(d.start); });";

            html += @"svg.append('g')
                    .attr('id', 'bars')
                    .selectAll('rect')
                    .data(data.filter(function(d){return d.switch > limit;}))
                    .enter()
                    .append('rect')
                    .attr({'x':function(d) {return x(d.start);},'y':function(d){ return y0(d.switch); } })
					.style('fill', 'url(#error-pattern)')
                    .attr('height', function(d) {return xAxisYPosition - y0(d.switch);})
					.attr('width', function(d){ return x(d.end) - x(d.start); });";

            //Draw circle
            html += @"svg.selectAll('circle')
                    .data(data.filter(function(d) {return d.switch == limit; }))
                    .enter().append('svg:circle')
                    .attr('cx', function(d) { return x(d.start) })
                    .attr('cy', function(d) { return y0(d.switch) })
                    .attr('stroke-width', 'none')
                    .attr('fill', 'orange')
                    .attr('r', 5);";

            html += "</script>";

            return html;
        }

        private List<TimelineDataPoint> GenerateData(List<ActivityContext> activities, ContextCategory targetActivity)
        {
            TimeSpan sumTime = TimeSpan.FromTicks(0);
            int sumSwitches = 0;

            var dataPoints = new List<TimelineDataPoint>();

            for (int i = 0; i < activities.Count - 1; i++)
            {
                activities.ElementAt(i).End = activities.ElementAt(i + 1).Start;
            }
            
            activities.RemoveAll(a => a.Activity != targetActivity);

            foreach (var activity in activities)
            {
                sumTime += activity.Duration;
                sumSwitches += 1;
                dataPoints.Add(new TimelineDataPoint { Start = activity.Start, End = activity.End.HasValue ? activity.End.Value : DateTime.Now, SumTime = sumTime, SumSwitches = sumSwitches });
            }

            return dataPoints;
        }

        private String GenerateJSData(List<TimelineDataPoint> dataPoints)
        {
            string data = "var data = [";

            foreach (TimelineDataPoint dataPoint in dataPoints)
            {
                data +=  dataPoint + ",";
            }

            data = data.Substring(0, data.Length - 1);
            data += "];";

            return data;
        }
        
        public struct TimelineDataPoint
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public TimeSpan SumTime { get; set; }
            public int SumSwitches { get; set; }

            public override string ToString()
            {
                return "{'start':'" + Start.ToString("HH:mm") + "', 'end': '" + End.ToString("HH:mm") + "', 'time': " + SumTime.Minutes + ", 'switch': " + SumSwitches + "}";
            }
        }
    }
}