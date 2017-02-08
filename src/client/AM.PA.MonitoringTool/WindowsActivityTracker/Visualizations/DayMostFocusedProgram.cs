// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-12-09
// 
// Licensed under the MIT License.

using Shared;
using Shared.Helpers;
using System;
using System.Globalization;
using WindowsActivityTracker.Data;

namespace WindowsActivityTracker.Visualizations
{
    internal class DayMostFocusedProgram : BaseVisualization, IVisualization
    {
        private readonly DateTimeOffset _date;
        private const int _minFocusTime = 2;

        public DayMostFocusedProgram(DateTimeOffset date)
        {
            this._date = date;

            Title = "Longest Time<br />Focused in a Program";
            IsEnabled = true; //todo: handle by user
            Order = 20; //todo: handle by user
            Size = VisSize.Small;
            Type = VisType.Day;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            /////////////////////
            // fetch data sets
            /////////////////////
            var queryResultsLocal = Queries.GetLongestFocusOnProgram(_date);
            var durInMin = (queryResultsLocal == null) ? 0 : queryResultsLocal.DurationInSec / 60.0;

            if (queryResultsLocal == null || durInMin <= _minFocusTime)
            {
                html += VisHelper.NotEnoughData(string.Format(CultureInfo.InvariantCulture, "We either don't have enough data or you didn't focus on a single program for more than {0} minutes on this day.", _minFocusTime));
                return html;
            }

            /////////////////////
            // HTML
            /////////////////////
            html += "<p style='text-align: center; margin-top:-0.7em;'><strong style='font-size:2.5em; color:" + Shared.Settings.RetrospectionColorHex + ";'>" + Math.Round(durInMin, 0) + "</strong>min</p>";
            html += string.Format(CultureInfo.InvariantCulture, "<p style='text-align: center; margin-top:-0.7em;'>in {0}<br />from {1} to {2}</p>",
                ProcessNameHelper.GetFileDescription(queryResultsLocal.Process),
                queryResultsLocal.From.ToShortTimeString(),
                queryResultsLocal.To.ToShortTimeString());

            return html;
        }
    }
}
