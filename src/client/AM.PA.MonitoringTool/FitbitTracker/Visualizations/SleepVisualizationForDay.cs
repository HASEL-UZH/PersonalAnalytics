// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-04
// 
// Licensed under the MIT License.

using System;
using Shared;
using FitbitTracker.Data;
using Shared.Helpers;

namespace FitbitTracker
{
    internal class SleepVisualizationForDay : BaseVisualization, IVisualization
    {
        private DateTimeOffset date;

        public SleepVisualizationForDay(DateTimeOffset date)
        {
            Title = "Sleep stats";
            this.date = date;
            IsEnabled = true;
            Size = VisSize.Square;
            Order = 0;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            SleepVisualizationEntry value = DatabaseConnector.GetSleepDataForDay(DateTimeHelper.GetStartOfDay(date), DateTimeHelper.GetEndOfDay(date));

            if (value == null)
            {
                html += VisHelper.NotEnoughData();
                return html;
            }

            //HTML
            html += "<div id='chart' height=20em, width=50em, style='align: center'></div>";
            html += "<p style='text-align: center; font-size: 0.66em;'>Hint: Visualizes your sleep stats for today. For more detailed information, visit: <a href='http://fitbit.com' target=_blank>fitbit.com</a>. (Last synced: " + DatabaseConnector.GetLastTimeSynced() + ").</p>";

            return html;
        }

    }

    public class SleepVisualizationEntry
    {
        public int SleepDuration { get; set; }

        public int AwakeCount { get; set; }

        public int AwakeDuration { get; set; }

        public int RestlessCount { get; set; }

        public int RestlessDuration { get; set; }
    }

}