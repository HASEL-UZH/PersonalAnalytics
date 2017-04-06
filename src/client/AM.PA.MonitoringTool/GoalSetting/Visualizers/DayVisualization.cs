// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-04-05
// 
// Licensed under the MIT License.

using GoalSetting.Goals;
using Shared;
using System;

namespace GoalSetting.Visualizers
{
    public abstract class DayVisualization : BaseVisualization, IVisualization
    {

        internal GoalActivity _goal;
        internal DateTimeOffset _date;

        public DayVisualization(DateTimeOffset date, GoalActivity goal)
        {
            Title = goal.ToString();
            this._goal = goal;
            this._date = date;
            IsEnabled = true;
            Size = VisSize.Wide;
            Order = 0;
        }

        public abstract override string GetHtml();

    }
}