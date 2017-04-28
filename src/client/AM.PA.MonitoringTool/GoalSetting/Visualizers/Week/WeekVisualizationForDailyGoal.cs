// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-27
// 
// Licensed under the MIT License.

using System;
using GoalSetting.Goals;
using Shared.Helpers;
using Shared;
using System.Collections.Generic;
using System.Linq;
using Shared.Data;
using GoalSetting.Model;
using GoalSetting.Data;

namespace GoalSetting.Visualizers.Week
{
    internal class WeekVisualizationForDailyGoal : PAVisualization
    {
        public WeekVisualizationForDailyGoal(DateTimeOffset date, GoalActivity goal) : base(date, goal) { }

        double numberSuccess = 0;
        double numberTries = 0;

        public override string GetHtml()
        {

            var html = string.Empty;
            var startOfWork = DateTimeHelper.GetFirstDayOfWeek_Iso8801(_date).DateTime;
            var endOfWork = DateTime.Now;

            var activities = DatabaseConnector.GetActivitiesSinceAndBefore(startOfWork, endOfWork);
            activities = DataHelper.MergeSameActivities(activities, Settings.MinimumSwitchTimeInSeconds);

            var targetActivity = _goal.Activity;
            var dataPoints = GenerateData(activities, targetActivity, startOfWork, endOfWork);

            // CSS
            html += "<style type='text/css'>";
            html += ".c3-line { stroke-width: 2px; }";
            html += ".c3-grid text, c3.grid line { fill: black; }";
            html += ".axis path, .axis line {fill: none; stroke: black; stroke-width: 1; shape-rendering: crispEdges;}";
            html += "</style>";

            //HTML
            html += "<div id='" + VisHelper.CreateChartHtmlTitle(Title) + "' style='align: center'></div>";
            html += "<p style='text-align: center; font-size: 0.66em;'>You achieved your goal in " + numberSuccess + " of " + numberTries + " (" + (numberSuccess / numberTries * 100.0).ToString("N2") + "%) cases.</p>";

            //JS
            html += "<script>";
            
            html += GenerateJSData(dataPoints);

            if (_goal.Rule.Goal == RuleGoal.TimeSpentOn)
            {
                html += "var xData = ['belowLimitTime', 'aboveLimitTime'];";
            }
            else
            {
                html += "var xData = ['belowLimitSwitches', 'aboveLimitSwitches'];";
            }

            html += "var actualHeight = document.getElementsByClassName('item Wide')[0].offsetHeight;";
            html += "var actualWidth = document.getElementsByClassName('item Wide')[0].offsetWidth;";
            html += "var margin = {top: 30, right: 30, bottom: 30, left: 40}, width = (actualWidth * 0.97)- margin.left - margin.right, height = (actualHeight * 0.73) - margin.top - margin.bottom;";

            html += @"var x = d3.scale.ordinal().rangeRoundBands([0, width], .35);
            
                    var y = d3.scale.linear().rangeRound([height, 0]);";

            html += "var limit = " + GoalVisHelper.GetLimitValue(_goal, VisType.Week) + ";";
            
            if (_goal.Rule.Goal == RuleGoal.TimeSpentOn)
            {
                html += "var color = d3.scale.ordinal().domain(['belowLimitTime', 'aboveLimitTime']).range(['" + Color1 + "', '" + Color2 + "']);";
            }
            else
            {
                html += "var color = d3.scale.ordinal().domain(['belowLimitSwitches', 'aboveLimitSwitches']).range(['" + Color1 + "', '" + Color2 + "']);";
            }

            html += @"var xAxis = d3.svg.axis().scale(x).orient('bottom');

                    var yAxis = d3.svg.axis().scale(y).orient('left');";

            html += "var svg = d3.select('#" + VisHelper.CreateChartHtmlTitle(Title) + "').append('svg')";

            html += @".attr('width', width + margin.left + margin.right).attr('height', height + margin.top + margin.bottom)
                    .append('g').attr('transform', 'translate(' + margin.left + ',' + margin.top + ')');
                    
                    var dataIntermediate = xData.map(function(c) {
                        return data.map(function(d) {
                            return {x: d.start, y: d[c]};
                        });
                    });

                    var dataStackLayout = d3.layout.stack()(dataIntermediate);

                    x.domain(dataStackLayout[0].map(function (d) {
                        return d.x;
                    }));

                    y.domain([0, Math.max(d3.max(dataStackLayout[dataStackLayout.length - 1],
                        function(d) { return d.y0 + d.y;}), limit)
                    ]).nice();

                    var layer = svg.selectAll('.stack')
                    .data(dataStackLayout)
                    .enter().append('g')
                    .attr('class', 'stack')
                    .style('fill', function(d, i) {
                        return color(i);
                    });

                    layer.selectAll('rect').data(function (d) {
                        return d;
                    })
                    .enter().append('rect')
                    .attr('x', function (d) {
                        return x(d.x);
                    })
                    .attr('y', function (d) {
                        return y(d.y + d.y0);
                    })
                    .attr('height', function (d) {
                        return y(d.y0) - y(d.y + d.y0);
                    })
                    .attr('width', x.rangeBand());";

            html += "var limit = " + GoalVisHelper.GetLimitValue(_goal, VisType.Week) + ";";

            html += "svg.append('text').attr('x', 0).attr('y', -10).style('text-anchor', 'middle').style('font-size', '0.5em').text('Time spent (h)');";

            html += "svg.append('line').style('stroke-dasharray', ('3, 3')).style('stroke', '" + Shared.Settings.RetrospectionColorHex + "').attr('x1', 0).attr('y1', y(limit)).attr('x2', d3.max(data, function(d){return x(d.start);})).attr('y2', y(limit));";

            html += @"svg.append('g').attr('class', 'axis').attr('transform', 'translate(0,' + height + ')').call(xAxis);
                    svg.append('g').attr('class', 'axis').attr('transform', 'translate(' + width + ', height)').call(yAxis);";

            html += "</script>";

            return html;
        }

        private String GenerateJSData(List<TimelineDataPoint> dataPoints)
        {
            string data = "var data = [";

            bool hasData = false;
            foreach (TimelineDataPoint dataPoint in dataPoints)
            {
                data += dataPoint + ",";
                hasData = true;
            }

            if (hasData)
            {
                data = data.Substring(0, data.Length - 1);
            }
            data += "];";

            return data;
        }

        private List<TimelineDataPoint> GenerateData(List<ActivityContext> activities, ContextCategory targetActivity, DateTime start, DateTime end)
        {
            var dataPoints = new List<TimelineDataPoint>();
            activities.RemoveAll(a => a.Activity != targetActivity);

            DateTime startDate = start;
            DateTime endDate = DateTime.Now.AddDays(1);

            //Iterate over each day in week
            while (endDate.Day != startDate.Day)
            {
                var dayActivities = activities.Where(a => a.Start.Day == startDate.Day).ToList();
                
                var switches = 0;
                var timeSpent = TimeSpan.FromMinutes(0);
                foreach (var activity in dayActivities)
                {
                    if (activity.End.Value.Day == activity.Start.Day)
                    {
                        switches += 1;
                        timeSpent += activity.Duration;
                    }
                }

                var aboveTimeLimit = TimeSpan.FromMilliseconds(0);
                var aboveSwitchLimit = 0;

                var belowTimeLimit = TimeSpan.FromMilliseconds(0);
                var belowSwitchLimit = 0;

                var limitSwitch = Int32.Parse(_goal.Rule.TargetValue);
                var limitTime = TimeSpan.FromMilliseconds(double.Parse(_goal.Rule.TargetValue));

                if (_goal.Rule.Goal == RuleGoal.NumberOfSwitchesTo)
                {
                    numberTries++;

                    //Switch
                    if (switches > limitSwitch)
                    {
                        if (_goal.Rule.Operator == RuleOperator.GreaterThan)
                        {
                            numberSuccess++;
                        }
                        aboveSwitchLimit = switches - limitSwitch;
                        belowSwitchLimit = switches;
                    }
                    else
                    {
                        if (_goal.Rule.Operator == RuleOperator.LessThan && switches < limitSwitch)
                        {
                            numberSuccess++;
                        }
                        if (_goal.Rule.Operator == RuleOperator.Equal && switches == limitSwitch)
                        {
                            numberSuccess++;
                        }
                        aboveSwitchLimit = 0;
                        belowSwitchLimit = limitSwitch;
                    }
                }
                else if (_goal.Rule.Goal == RuleGoal.TimeSpentOn)
                {
                    numberTries++;

                    //Time
                    if (timeSpent > limitTime)
                    {
                        if (_goal.Rule.Operator == RuleOperator.GreaterThan)
                        {
                            numberSuccess++;
                        }
                        aboveTimeLimit = timeSpent - limitTime;
                        belowTimeLimit = limitTime;
                    }
                    else 
                    {
                        if (_goal.Rule.Operator == RuleOperator.LessThan && switches < limitSwitch)
                        {
                            numberSuccess++;
                        }
                        if (_goal.Rule.Operator == RuleOperator.Equal && switches == limitSwitch)
                        {
                            numberSuccess++;
                        }
                        aboveTimeLimit = TimeSpan.FromMilliseconds(0);
                        belowTimeLimit = timeSpent;
                    }
                }

                dataPoints.Add(new TimelineDataPoint { Start = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0, 0), End = new DateTime(startDate.Year, startDate.Month, startDate.Day, 23, 59, 59, 999), TotalSwitches = switches, TotalTime = timeSpent, BelowLimitTime = belowTimeLimit, AboveLimitTime = aboveTimeLimit, AboveLimitSwitches = aboveSwitchLimit, BelowLimitSwitches = belowSwitchLimit });

                startDate = startDate.AddDays(1);
            }

            return dataPoints;
        }
    }

    public struct TimelineDataPoint
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public int TotalSwitches { get; set; }
        public int BelowLimitSwitches { get; set; }
        public int AboveLimitSwitches { get; set; }
        public TimeSpan TotalTime { get; set; }
        public TimeSpan BelowLimitTime { get; set; }
        public TimeSpan AboveLimitTime { get; set; }

        public override string ToString()
        {
            return "{'start':'" + Start.ToString("ddd") + "', 'end': '" + End.ToString("ddd") + "', 'totalTime': " + TotalTime.TotalHours + ", 'aboveLimitTime': " + AboveLimitTime.TotalHours + ", 'belowLimitTime': " + BelowLimitTime.TotalHours + ", 'totalSwitch': " + TotalSwitches + ", 'aboveLimitSwitches': " + AboveLimitSwitches + ", 'belowLimitSwitches': " + BelowLimitSwitches + "}";
        }
    }
}