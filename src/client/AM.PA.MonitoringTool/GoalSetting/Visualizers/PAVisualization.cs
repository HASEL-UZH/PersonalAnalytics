// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-27
// 
// Licensed under the MIT License.

using System;
using Shared;
using GoalSetting.Goals;

namespace GoalSetting.Visualizers
{
    public abstract class PAVisualization : BaseVisualization, IVisualization
    {
        protected DateTimeOffset _date;
        protected GoalActivity _goal;

        public PAVisualization(DateTimeOffset date)
        {
            this._date = date;
            IsEnabled = true;
            Size = VisSize.Wide;
            Order = 0;
        }

        public PAVisualization(DateTimeOffset date, GoalActivity goal)
        {
            this._goal = goal;
            this._date = date;
            Title = goal.Title + ": " + goal.ToString();
            IsEnabled = true;
            Size = VisSize.Wide;
            Order = 0;
        }
        
        public abstract override string GetHtml();

    }
}