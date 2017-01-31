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
    internal class FitbitVisualizationForWeek : BaseVisualization, IVisualization
    {
        private DateTimeOffset date;

        public FitbitVisualizationForWeek(DateTimeOffset date)
        {
            this.date = date;
            Title = "Fitbit Visualization from " + DateTimeHelper.GetFirstDayOfWeek_Iso8801(date).ToString(Settings.FORMAT_DAY) + " to " + DateTimeHelper.GetLastDayOfWeek_Iso8801(date).ToString(Settings.FORMAT_DAY);
            IsEnabled = true;
            Order = 0;
        }

        public override string GetHtml()
        {
            return "<html>Hours asleep: " + DatabaseConnector.GetMinutesAsleep(DateTimeHelper.GetFirstDayOfWeek_Iso8801(date), DateTimeHelper.GetLastDayOfWeek_Iso8801(date), VisType.Week) / 60.0 + "</html>";
        }

    }
}