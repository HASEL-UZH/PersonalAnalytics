// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-30
// 
// Licensed under the MIT License.

using System;
using Shared;
using FitbitTracker.Data;
using Shared.Helpers;

namespace FitbitTracker
{
    internal class FitbitVisualizationForDay : BaseVisualization, IVisualization
    {
        private DateTimeOffset date;

        public FitbitVisualizationForDay(DateTimeOffset date)
        {
            Title = "Fitbit Visualization for " + date.ToString(Settings.FORMAT_DAY);
            this.date = date;
            IsEnabled = true;
            Order = 0;
        }

        public override string GetHtml()
        {
            return "<html>Hours asleep: " + DatabaseConnector.GetMinutesAsleep(DateTimeHelper.GetStartOfDay(date), DateTimeHelper.GetEndOfDay(date)) / 60.0  + "</html>";
        }

    }

}