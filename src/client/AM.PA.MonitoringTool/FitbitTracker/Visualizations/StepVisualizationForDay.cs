// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-30
// 
// Licensed under the MIT License.

using System;
using Shared;
using FitbitTracker.Data;
using Shared.Helpers;
using System.Collections.Generic;

namespace FitbitTracker
{
    internal class StepVisualizationForDay : BaseVisualization, IVisualization
    {
        private DateTimeOffset date;

        public StepVisualizationForDay(DateTimeOffset date)
        {
            Title = "Steps per 15 minute";
            this.date = date;
            IsEnabled = true;
            Size = VisSize.Wide;
            Order = 0;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            List<Tuple<DateTime, int>> values = DatabaseConnector.GetStepsPerTimeFraction(DateTimeHelper.GetStartOfDay(date), DateTimeHelper.GetEndOfDay(date), 15);

            if (values.Count <= 1)
            {
                html += VisHelper.NotEnoughData();
                return html;
            }

            //HTML
            html += "<div id='chart' height=20em, width=50em, style='align: center'></div>";
            html += "<p style='text-align: center; font-size: 0.66em;'>Hint: Visualizes your steps per 15 minutes. For more detailed information, visit: <a href='http://fitbit.com' target=_blank>fitbit.com</a>. (Last synced: " + DatabaseConnector.GetLastTimeSynced() + ").</p>";

            return html;
        }

    }

}