// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-27
// 
// Licensed under the MIT License.

using System;
using GoalSetting.Rules;
using Shared;
using Shared.Helpers;

namespace GoalSetting.Visualizers
{
    internal class WeekVisualization : BaseVisualization, IVisualization
    {
        private DateTimeOffset _date;
        private PARule _rule;

        public WeekVisualization(DateTimeOffset date, PARule rule)
        {
            Title = "Goal: " + rule.Title;
            this._rule = rule;
            this._date = date;
            IsEnabled = true;
            Size = VisSize.Square;
            Order = 0;
        }

        public override string GetHtml()
        {
            var html = string.Empty;
            html += VisHelper.NotEnoughData();
            return html;
        }
    }
}