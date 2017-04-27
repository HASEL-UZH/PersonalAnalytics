// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-04-05
// 
// Licensed under the MIT License.

using GoalSetting.Data;
using Shared;
using Shared.Data;
using Shared.Helpers;
using System;
using GoalSetting.Model;
using System.Collections.Generic;
using System.Linq;
using GoalSetting.Goals;

namespace GoalSetting.Visualizers.Day
{
    public class DayVisualizationForHourlyGoals : PAVisualization
    {
        public DayVisualizationForHourlyGoals(DateTimeOffset date, GoalActivity goal) : base(date, goal) { }

        public override string GetHtml()
        {

            var html = string.Empty;
            var startOfWork = Database.GetInstance().GetUserWorkStart(DateTime.Now.Date);
            var endOfWork = DateTime.Now;

            if (endOfWork.Subtract(startOfWork).TotalMinutes <= 30)
            {
                return VisHelper.NotEnoughData();
            }

            var activities = DatabaseConnector.GetActivitiesSinceAndBefore(startOfWork, endOfWork);
            activities = DataHelper.MergeSameActivities(activities, Settings.MinimumSwitchTimeInSeconds);

            var targetActivity = _goal.Activity;

            // CSS
            html += "<style type='text/css'>";
            html += ".c3-line { stroke-width: 2px; }";
            html += ".c3-grid text, c3.grid line { fill: black; }";
            html += ".axis path, .axis line {fill: none; stroke: black; stroke-width: 1; shape-rendering: crispEdges;}";
            html += "</style>";

            //HTML
            html += "<div id='" + VisHelper.CreateChartHtmlTitle(Title) + "' style='align: center'></div>";
            html += "<p style='text-align: center; font-size: 0.66em;'>" + _goal.GetProgressMessage() + "</p>";

            //JS
            html += "<script>";

            var dataPoints = GenerateData(activities, targetActivity, startOfWork, endOfWork);
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

            if (_goal.Rule.Goal == RuleGoal.TimeSpentOn)
            {
                html += "var color = d3.scale.ordinal().domain(['belowLimitTime', 'aboveLimitTime']).range(['" + GoalVisHelper.GetVeryHighColor() + "', '" + GoalVisHelper.GetVeryLowColor() + "']);";
            }
            else
            {
                html += "var color = d3.scale.ordinal().domain(['belowLimitSwitches', 'aboveLimitSwitches']).range(['" + GoalVisHelper.GetVeryHighColor() + "', '" + GoalVisHelper.GetVeryLowColor() + "']);";
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

                    y.domain([0, d3.max(dataStackLayout[dataStackLayout.length - 1],
                        function(d) { return d.y0 + d.y;})
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

            html += "var limit = " + GoalVisHelper.GetLimitValue(_goal, VisType.Day) + ";";

            html += "svg.append('text').attr('x', 0).attr('y', -10).style('text-anchor', 'middle').style('font-size', '0.5em').text('" + GoalVisHelper.GetXAxisTitle(_goal, VisType.Day) + "');";

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
            DateTime endDate = end.AddHours(1);

            //Iterate over each hour
            while (endDate.Hour != startDate.Hour)
            {
                var hourActivities = activities.Where(a => a.Start.Hour == startDate.Hour).ToList();

                var switches = 0;
                var timeSpent = TimeSpan.FromMinutes(0);
                foreach (var activity in hourActivities)
                {
                    switches += 1;

                    if (activity.End.Value.Hour == start.Hour)
                    {
                        var durationInThisHour = startDate.AddHours(1).Subtract(activity.Start);
                        activity.Start = activity.Start.AddHours(1);
                        timeSpent += durationInThisHour;
                    }
                    else if (activity.End.Value.Hour > start.Hour)
                    {
                        timeSpent += activity.Duration;
                    }
                }
                
                var timeOverLimit = timeSpent.Subtract(TimeSpan.FromMilliseconds(double.Parse(_goal.Rule.TargetValue)));
                timeOverLimit = timeOverLimit.TotalMinutes >= 0 ? timeOverLimit : TimeSpan.FromMinutes(0);
                var timeBelowLimit = timeSpent.Subtract(timeOverLimit);
                
                var switchLimit = Int32.Parse(_goal.Rule.TargetValue);

                var switchesBelowLimit = switches < switchLimit ? switches : switchLimit;
                var switchesAboveLimit = switches > switchLimit ? switches - switchLimit : 0;
                
                dataPoints.Add(new TimelineDataPoint { Start = new DateTime(startDate.Year, startDate.Month, startDate.Day, startDate.Hour, 0, 0, 0), End = new DateTime(startDate.Year, startDate.Month, startDate.Day, startDate.Hour, 59, 59, 999), TotalSwitches = switches, TotalTime = timeSpent, BelowLimitTime = timeBelowLimit, AboveLimitTime = timeOverLimit, AboveLimitSwitches = switchesAboveLimit, BelowLimitSwitches = switchesBelowLimit });

                startDate = startDate.AddHours(1);
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
            return "{'start':'" + Start.ToString("HH:mm") + "', 'end': '" + End.ToString("HH:mm") + "', 'totalTime': " + TotalTime.TotalMinutes + ", 'aboveLimitTime': " + AboveLimitTime.TotalMinutes + ", 'belowLimitTime': " + BelowLimitTime.TotalMinutes + ", 'totalSwitch': " + TotalSwitches + ", 'aboveLimitSwitches': " + AboveLimitSwitches + ", 'belowLimitSwitches': " + BelowLimitSwitches + "}";
        }
    }
}