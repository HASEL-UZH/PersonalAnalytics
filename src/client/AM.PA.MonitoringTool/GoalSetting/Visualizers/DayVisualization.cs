// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-27
// 
// Licensed under the MIT License.

using Shared;
using Shared.Helpers;
using System;
using GoalSetting.Rules;

namespace GoalSetting.Visualizers
{
    public class DayVisualization : BaseVisualization, IVisualization
    {
        private PARule _rule;
        private DateTimeOffset _date;
        
        public DayVisualization(DateTimeOffset date, PARule rule)
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