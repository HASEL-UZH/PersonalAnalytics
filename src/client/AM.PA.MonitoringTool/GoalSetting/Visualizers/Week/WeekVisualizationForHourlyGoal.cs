// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-04-13
// 
// Licensed under the MIT License.

using System;
using GoalSetting.Goals;
using GoalSetting.Model;
using Shared;
using Shared.Helpers;
using GoalSetting.Data;
using System.Linq;
using System.Collections.Generic;

namespace GoalSetting.Visualizers.Week
{
    public class WeekVisualizationForHourlyGoal : PAVisualization
    {
        public WeekVisualizationForHourlyGoal(DateTimeOffset date, GoalActivity goal) : base(date, goal) { }

        double numberSuccess = 0;
        double numberTries = 0;
        List<ActivityContext> allActivities = null;
        public override string GetHtml()
        {
            var html = string.Empty;

            var startOfWork = DateTimeHelper.GetFirstDayOfWeek_Iso8801(_date).DateTime;
            var endOfWork = DateTime.Now;
            allActivities = DatabaseConnector.GetActivitiesSinceAndBefore(startOfWork, endOfWork);
            allActivities = DataHelper.MergeSameActivities(allActivities, Settings.MinimumSwitchTimeInSeconds);
            allActivities = allActivities.Where(a => a.Activity.Equals(base._goal.Activity)).ToList();

            var dataString = GenerateData();

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

            html += "var actualHeight = document.getElementsByClassName('item Wide')[0].offsetHeight;";
            html += "var actualWidth = document.getElementsByClassName('item Wide')[0].offsetWidth;";
            html += "var margin = {top: 10, right: 30, bottom: 30, left: 30}, width = (actualWidth * 0.97)- margin.left - margin.right, height = (actualHeight * 0.73) - margin.top - margin.bottom;";

            html += dataString;

            html += "var grid = d3.select('#" + VisHelper.CreateChartHtmlTitle(Title) + "').append('svg')";
            html += @".attr('width', width + margin.left + margin.right).attr('height', height + margin.top + margin.bottom)
                    .append('g').attr('transform', 'translate(' + margin.left + ',' + margin.top + ')');";

            html += "var row = grid.selectAll('.row')";
            html += ".data(gridData)";
            html += ".enter().append('g')";
            html += ".attr('class', 'row');";

            html += "var column = row.selectAll('.square')";
            html += ".data(function(d) { return d; })";
            html += ".enter().append('rect')";
            html += ".attr('class', 'square')";
            html += ".attr('x', function(d) { return d.x * (width / 25); })";
            html += ".attr('y', function(d) { return d.y * (height / 8); })";
            html += ".attr('width', (width / 25))";
            html += ".attr('height', (height / 8))";
            html += @".style('fill', function(d) {
                if (d.type === 'Title') {";
            html += "return '" + Shared.Settings.GrayColorHex + "';";
            html += @"} else if (d.hasValue === 'False') {
                    return '#fff';
                } else {
                    if (d.success === 'True') {";
            html += "return '" + Shared.Settings.RetrospectionColorHex + "';";
            html += "}";
            html += "return 'red';}";
            html += "return '#fff';})";
            html += ".style('stroke', '" + Shared.Settings.GrayColorHex + "');";

            html += "var text = row.selectAll('.label')";
            html += ".data(function(d) { return d; })";
            html += ".enter().append('svg:text')";
            html += ".attr('x', function(d) { return (d.x * (width / 25)) + ((width / 25) / 2); })";
            html += ".attr('y', function(d) { return (d.y * (height / 8)) + ((height / 8) / 2); })";
            html += ".attr('text-anchor', 'middle')";
            html += ".attr('dy', '.35em')";
            html += ".attr('stroke', function(d) { if (d.type === 'Title') { return 'black';} return 'white';})";
            html += ".text(function(d) { return d.value });";

            html += "</script>";

            return html;
        }

        private string GenerateData()
        {
            var html = string.Empty;

            html += "var gridData = [";

            double xPos = 1;
            double yPos = 1;
            string newValue = "";
            string newType = "";
            bool success = false;
            bool hasValue = false;

            for (var row = 0; row < 8; row++)
            {
                html += "[";
                
                for (var column = 0; column < 25; column++)
                {
                    if (row == 0)
                    {
                        if (column != 0)
                        {
                            newValue = "" + (column - 1);
                        }
                    }
                    else if (column == 0)
                    {
                        if (row == 1)
                        {
                            newValue = "Mo";
                        }
                        if (row == 2)
                        {
                            newValue = "Di";
                        }
                        if (row == 3)
                        {
                            newValue = "Mi";
                        }
                        if (row == 4)
                        {
                            newValue = "Do";
                        }
                        if (row == 5)
                        {
                            newValue = "Fr";
                        }
                        if (row == 6)
                        {
                            newValue = "Sa";
                        }
                        if (row == 7)
                        {
                            newValue = "So";
                        }
                    }
                    else
                    {
                        var firstDayOfWeek = DateTimeHelper.GetFirstDayOfWeek_Iso8801(base._date);
                        firstDayOfWeek = new DateTime(firstDayOfWeek.Year, firstDayOfWeek.Month, firstDayOfWeek.Day, column - 1, 0, 0, 0);
                        var dateToCheck = firstDayOfWeek.AddDays(row - 1);
                        var dateToCheckEnd = dateToCheck.AddMinutes(59);
                        dateToCheckEnd = dateToCheckEnd.AddSeconds(59);

                        if (dateToCheck > DateTime.Now)
                        {
                            newValue = "";
                            hasValue = false;
                        }
                        else
                        {
                            numberTries++;
                            Tuple<string, bool> values = GetValue(dateToCheck, dateToCheckEnd);
                            newValue = values.Item1;
                            success = values.Item2;
                            if (success)
                            {
                                numberSuccess++;
                            }
                            hasValue = true;
                        }
                    }

                    if (row == 0 || column == 0)
                    {
                        newType = "Title";
                    }
                    else
                    {
                        newType = "Value";
                    }

                    html += "{type:'" + newType + "', x:" + xPos + ", y:" + yPos + ", value:'" + newValue + "', success: '" + success + "', hasValue: '" + hasValue + "'},";
                    xPos += 1;

                }
                html = html.Remove(html.Length - 1);
                html += "],";
                xPos = 1;
                yPos += 1;
            }

            html = html.Remove(html.Length - 1);
            html += "];";
            
            return html;
        }

        private Tuple<string, bool> GetValue(DateTimeOffset start, DateTimeOffset end)
        {
            var activities = new List<ActivityContext>();
            foreach (var activity in allActivities)
            {
                var timeNextDay = new DateTime(end.Year, end.Month, end.Day + 1, 4, 0, 0);
                var timePreviousDay = new DateTime(start.Year, start.Month, start.Day - 1, 23, 59, 59);
                if (activity.End > timeNextDay || activity.Start < timePreviousDay)
                {
                    continue;
                }

                if (activity.Start > start && activity.End < end)
                {
                    activities.Add(activity);
                }
                else if (activity.Start < start && activity.End > start && activity.End < end)
                {
                    activities.Add(new ActivityContext { Start = start.DateTime, End = activity.End, Activity = activity.Activity});
                }
                else if (activity.Start > start && activity.Start < end && activity.End > end)
                {
                    activities.Add(new ActivityContext { Start = activity.Start, End = end.DateTime, Activity = activity.Activity });
                }
                else if (activity.Start < start && activity.End > end)
                {
                    activities.Add(new ActivityContext { Start = start.DateTime, End = end.DateTime, Activity = activity.Activity });
                }
            }
            
            if (activities.Count < 1)
            {
                return Tuple.Create<string, bool>("0", true);
            }

            switch (base._goal.Rule.Goal)
            {
                case RuleGoal.NumberOfSwitchesTo:
                    int numberOfSwitches = DataHelper.GetNumberOfSwitchesToActivity(activities, base._goal.Activity);
                    return Tuple.Create<string, bool>("" + numberOfSwitches, DataHelper.SuccessRule(base._goal.Rule, numberOfSwitches));
                case RuleGoal.TimeSpentOn:
                    double timeSpentOn = DataHelper.GetTotalTimeSpentOnActivity(activities, base._goal.Activity).TotalMilliseconds;
                    return Tuple.Create<string, bool>(DataHelper.GetTotalTimeSpentOnActivity(activities, base._goal.Activity).TotalMinutes.ToString("N0"), DataHelper.SuccessRule(base._goal.Rule, timeSpentOn));
            }
            return Tuple.Create<string, bool>("", false);
        }
    }
}