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
            html += "<div id='chart' style='align: center; font-size: 1.15em;'>";
            html += "<p><b>Start:</b> " + value.StartTime.ToString(Settings.FORMAT_TIME) + "<span style='padding-left: 2em;'><b>End:</b> " + value.StartTime.AddMinutes(value.SleepDuration + value.AwakeDuration + value.RestlessDuration + value.AwakeAfterWakeUp).ToString(Settings.FORMAT_TIME) + "</span><span style='padding-left: 2em;'><b>Efficiency:</b> " + value.Efficiency + "%</span></p>";
            html += "<p><b>Slept for:</b> " + DurationToTime(value.SleepDuration) + "</p>";
            html += "<p><b>Time in bed after wakeup:</b> " + value.AwakeAfterWakeUp + " minutes</p>";
            html += "<p><b>Awake:</b> " + value.AwakeCount + " time(s). (Total duration: " + value.AwakeDuration + " minutes" + ")</p>";
            html += "<p><b>Restless:</b> " + value.RestlessCount + " time(s). (Total duration: " + value.RestlessDuration + " minutes" + ")</p>";
            html += "</div>";
            html += "<p style='text-align: center; font-size: 0.66em;'>Hint: Visualizes your sleep stats for the chosen day. For more detailed information,<br>visit: <a href='http://fitbit.com' target=_blank>fitbit.com</a>. (Last synced: " + DatabaseConnector.GetLastTimeSynced() + ").</p>";

            return html;
        }
        
        private string DurationToTime(int duration)
        {
            TimeSpan timeSpan = TimeSpan.FromMinutes(duration);
            return timeSpan.Hours.ToString("D2") + " hours, " + timeSpan.Minutes.ToString("D2") + " minutes";
        }

    }

    public class SleepVisualizationEntry
    {
        public DateTime StartTime { get; set; }

        public int SleepDuration { get; set; }

        public int AwakeCount { get; set; }

        public int AwakeDuration { get; set; }

        public int RestlessCount { get; set; }

        public int RestlessDuration { get; set; }

        public int AwakeAfterWakeUp { get; set; }

        public int Efficiency { get; set; }
    }

}