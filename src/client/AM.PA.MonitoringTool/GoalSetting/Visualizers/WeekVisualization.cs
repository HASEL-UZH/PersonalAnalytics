// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-27
// 
// Licensed under the MIT License.

using System;
using Shared;
using Shared.Helpers;
using GoalSetting.Goals;

namespace GoalSetting.Visualizers
{
    internal class WeekVisualization : BaseVisualization, IVisualization
    {
        private DateTimeOffset _date;
        private GoalActivity _goal;

        public WeekVisualization(DateTimeOffset date, GoalActivity goal)
        {
            Title = goal.ToString();
            this._goal = goal;
            this._date = date;
            IsEnabled = true;
            Size = VisSize.Wide;
            Order = 0;
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
            html += "<p style='text-align: center; font-size: 0.66em;'>" + GoalVisHelper.getHintText(_goal, VisType.Week) + "</p>";

            //JS
            html += "<script>";
            html += "</script>";

            return html;
        }
    }
}