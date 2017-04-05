// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-04-05
// 
// Licensed under the MIT License.

using GoalSetting.Rules;
using Shared;
using System;

namespace GoalSetting.Visualizers
{
    public abstract class DayVisualization : BaseVisualization, IVisualization
    {

        internal PARuleActivity _rule;
        internal DateTimeOffset _date;

        public DayVisualization(DateTimeOffset date, PARuleActivity rule)
        {
            Title = rule.ToString();
            this._rule = rule;
            this._date = date;
            IsEnabled = true;
            Size = VisSize.Wide;
            Order = 0;
        }

        public abstract override string GetHtml();

    }
}