// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-04-12
// 
// Licensed under the MIT License.

using System;
using GoalSetting.Goals;
using Shared;
using Shared.Helpers;

namespace GoalSetting.Visualizers.Week
{
    internal class WeekVisualizationForWeeklyGoal : PAVisualization
    {
        public WeekVisualizationForWeeklyGoal(DateTimeOffset date, GoalActivity goal) : base(date, goal) {
            Order = Int32.MinValue;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            // CSS
            html += "<style type='text/css'>";
            html += ".c3-line { stroke-width: 2px; }";
            html += ".c3-grid text, c3.grid line { fill: black; }";
            html += ".axis path, .axis line {fill: none; stroke: black; stroke-width: 1; shape-rendering: crispEdges;}";
            html += "</style>";

            //HTML
            html += "<div id='" + VisHelper.CreateChartHtmlTitle(Title) + "' style='align: center'></div>";
            html += "<p style='text-align: center; font-size: 0.66em;'>" + GoalVisHelper.getHintText(_goal, VisType.Day) + "</p>";

            return html;
        }
    }
}