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
    internal class StepVisualizationForWeek : BaseVisualization, IVisualization
    {
        private DateTimeOffset _date;

        public StepVisualizationForWeek(DateTimeOffset date)
        {
            this._date = date;
            Title = "Steps per Week Day";
            IsEnabled = true;
            Size = VisSize.Wide;
            Order = 0;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            List<Tuple<DateTime, int>> values = DatabaseConnector.GetStepsPerTimeFraction(DateTimeHelper.GetFirstDayOfWeek_Iso8801(_date), DateTimeHelper.GetLastDayOfWeek_Iso8801(_date), 24 * 60);

            if (values.Count <= 1)
            {
                html += VisHelper.NotEnoughData();
                return html;
            }

            //HTML
            html += "<div id='chart' height=20em, width=50em, style='align: center'></div>";
            html += "<p style='text-align: center; font-size: 0.66em;'>Hint: Visualizes your steps per day. For more detailed information, visit: <a href='http://fitbit.com' target=_blank>fitbit.com</a>. (Last synced: " + DatabaseConnector.GetLastTimeSynced() + ").</p>";
            
            return html;
        }

    }
}