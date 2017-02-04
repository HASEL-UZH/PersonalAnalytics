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
    internal class SleepVisualizationForWeek : BaseVisualization, IVisualization
    {
        private DateTimeOffset date;

        public SleepVisualizationForWeek(DateTimeOffset date)
        {
            this.date = date;
            Title = "Sleep stats";
            Size = VisSize.Wide;
            IsEnabled = true;
            Order = 0;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            SleepVisualizationForWeekEntry value = DatabaseConnector.GetSleepDataForWeek(DateTimeHelper.GetFirstDayOfWeek_Iso8801(date), DateTimeHelper.GetLastDayOfWeek_Iso8801(date));

            if (value == null)
            {
                html += VisHelper.NotEnoughData();
                return html;
            }

            //HTML
            html += "<div id='chart' height=20em, width=50em, style='align: center'></div>";
            html += "<p style='text-align: center; font-size: 0.66em;'>Hint: Visualizes your sleep stats for this week. For more detailed information, visit: <a href='http://fitbit.com' target=_blank>fitbit.com</a>. (Last synced: " + DatabaseConnector.GetLastTimeSynced() + ").</p>";

            return html;
        }

    }

    public class SleepVisualizationForWeekEntry
    {
        public DateTime Day { get; set; }

        public int SleepDuration { get; set; }

        public int AwakeDuration { get; set; }

        public int RestlessDuration { get; set; }
    }

}