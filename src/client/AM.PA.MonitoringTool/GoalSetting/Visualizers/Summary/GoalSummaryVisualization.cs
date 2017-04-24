// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-27
// 
// Licensed under the MIT License.

using System;
using Shared.Helpers;
using GoalSetting.Goals;
using System.Collections.ObjectModel;

namespace GoalSetting.Visualizers.Summary
{
    public class GoalSummaryVisualization : PAVisualization
    {
        public GoalSummaryVisualization(DateTimeOffset date) : base(date) {
            Title = "Goal Summary";
            Order = -10;
        }

        public override string GetHtml()
        {
            var goals = GoalSettingManager.Instance.GetGoals();
            int numberOfItems = goals.Count;

            var html = string.Empty;

            if (goals.Count <= 0)
            {
                html += VisHelper.NotEnoughData();
                return html;
            }

            // CSS
            html += "<style type='text/css'>";
            
            html += @".bullet { font: 10px sans-serif;}
            .marker {stroke: #F00; stroke-width: 2px; }
            .tick line {stroke: #666; stroke-width: .5px; }
            .range.s0 {fill: #eee; }
            .range.s1 {fill: #ddd; }
            .range.s2 {fill: #ccc; }
            .measure.s0 {fill: steelblue;}
            .title {font - size: 14px; font - weight: bold; }
            .subtitle {fill: #999; }";
            
            html += "</style>";

            //HTML
            html += "<div id='" + VisHelper.CreateChartHtmlTitle(Title) + "' style='align: center'></div>";
            html += "<p style='text-align: center; font-size: 0.66em;'>" + "Goal Summary (blue: actual value, red: target value, bars: min, average, max)" + "</p>";

            //JS
            html += "<script src='bullet.js'></script>";
            html += "<script>";

            html += GenerateData(goals);
            
            html += "var actualHeight = document.getElementsByClassName('item Wide')[0].offsetHeight;";
            html += "var actualWidth = document.getElementsByClassName('item Wide')[0].offsetWidth;";
            html += "var margin = {top: 5, right: 40, bottom: 20, left: 120}, totalWidth = (actualWidth * 0.97)- margin.left - margin.right, totalHeight = (actualHeight * 0.73) - margin.top - margin.bottom;";

            html += "width = totalWidth,";
            html += "height = totalWidth / (6 * " + goals.Count + ");";

            html += "d3.select('#" + VisHelper.CreateChartHtmlTitle(Title) + "').attr('height', totalHeight);";

            html += "var chart = d3.bullet()";
            html += ".width(width)";
            html += ".height(height);";

            html += "var svg = d3.select('#" + VisHelper.CreateChartHtmlTitle(Title) + "').selectAll('svg')";
            html += @".data(data)
                    .enter().append('svg')
                    .attr('class', 'bullet')
                    .attr('width', width + margin.left + margin.right)
                    .attr('height', height + margin.top + margin.bottom)
                    .append('g')
                    .attr('transform', 'translate(' + margin.left + ',' + margin.top + ')')
                    .call(chart);";
            
            html += "var title = svg.append('g')";
            html += ".style('text-anchor', 'end')";
            html += ".attr('transform', 'translate(-6,' + height / 2 + ')');";

            html += "title.append('text')";
            html += ".attr('class', 'title')";
            html += ".text(function(d) { return d.title; });";

            html += "title.append('text')";
            html += ".attr('class', 'subtitle')";
            html += ".attr('dy', '1em')";
            html += ".text(function(d) { return d.subtitle; });";

            html += "title.append('svg:image')";
            html += ".attr('transform', 'translate(' + height * -3.5 + ',' + height / -2 + ')')";
            html += ".attr('xlink:href', function(d) {return d.image;})";
            html += ".attr('width', height)";
            html += ".attr('height', height);";

            html += "</script>";

            return html;
        }

        public string GenerateData(ObservableCollection<Goal> goals)
        {
            string data = string.Empty;

            data += "var data = [";
            
            foreach (Goal goal in goals)
            {
                goal.CalculateProgressStatus(false);
                data += "{";
                data += "'title':'" + goal.Title + "',";

                switch (goal.Rule.Goal)
                {
                    case Model.RuleGoal.NumberOfEmailsInInbox:
                        data += "'subtitle':'# Emails',";
                        break;

                    case Model.RuleGoal.NumberOfSwitchesTo:
                        data += "'subtitle':'# Switches',";
                        break;

                    case Model.RuleGoal.TimeSpentOn:
                        data += "'subtitle':'Time (h)',";
                        break;
                }

                switch (goal.Progress.Status)
                {
                    case ProgressStatus.VeryLow:
                        data += "'image':'smiley5.png',";
                        break;
                    case ProgressStatus.Low:
                        data += "'image':'smiley4.png',";
                        break;
                    case ProgressStatus.Average:
                        data += "'image':'smiley3.png',";
                        break;
                    case ProgressStatus.High:
                        data += "'image':'smiley2.png',";
                        break;
                    case ProgressStatus.VeryHigh:
                        data += "'image':'smiley1.png',";
                        break;
                }
                
                data += "'ranges':[" + DatabaseConnector.GetMinGoalValue(goal) + "," + DatabaseConnector.GetAverageGoalValue(goal) + "," + DatabaseConnector.GetMaxGoalValue(goal) + "],";
                data += "'measures':[" + goal.Progress.Actual + "],";
                data += "'markers':[" + goal.Progress.Target + "]";
                data += "}";
                data += ",";
            }

            data = data.Remove(data.Length - 1); 
           
            data += "];";

            return data;
        }
    }
}