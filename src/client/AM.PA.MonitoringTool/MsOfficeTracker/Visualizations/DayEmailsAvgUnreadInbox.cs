// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-12-11
// 
// Licensed under the MIT License.

using Shared;
using Shared.Helpers;
using System;
using MsOfficeTracker.Data;
using System.Globalization;

namespace MsOfficeTracker.Visualizations
{
    internal class DayEmailsAvgUnreadInbox : BaseVisualization, IVisualization
    {
        private readonly DateTimeOffset _date;

        public DayEmailsAvgUnreadInbox(DateTimeOffset date)
        {
            this._date = date;

            Title = "Avg Number of Unread Emails in the Inbox";
            IsEnabled = true; //todo: handle by user
            Order = 21; //todo: handle by user
            Size = VisSize.Small;
            Type = VisType.Day;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            /////////////////////
            // fetch data sets
            /////////////////////

            // get the latest stored email entry
            var inboxSizeResult = Queries.GetAverageInboxSize(_date);
            var inboxSize = inboxSizeResult.Item2;

            var isToday = (_date.Date == DateTime.Now.Date);
            var lastUpdatedMinsAgo = Math.Abs((DateTime.Now - inboxSizeResult.Item1).TotalMinutes);

            // if database entry is outdated or not there, create a live API call and override entries
            if (inboxSizeResult.Item1 == DateTime.MinValue || // no emails stored yet
                (isToday && lastUpdatedMinsAgo > Settings.SaveEmailCountsIntervalInMinutes) || // request is for today and saved results are too old
                (inboxSize == -1)) // could not fetch sent emails
            {
                // create and save a new email snapshot (inbox, sent, received)
                var res = Queries.CreateEmailsSnapshot(_date.Date, false);
                inboxSizeResult = Queries.GetAverageInboxSize(_date); // run query again
                inboxSize = inboxSizeResult.Item2;
            }

            // error (only if no data at all)
            if (inboxSize == -1)
            {
                return VisHelper.NotEnoughData(Dict.NotEnoughData);
            }

            // as a goodie get this too :)
            //var timeSpentInOutlook = Queries.TimeSpentInOutlook(_date);


            /////////////////////
            // HTML
            /////////////////////

            var emailInboxString = (inboxSize == -1) ? "?" : inboxSize.ToString(CultureInfo.InvariantCulture);

            html += "<p style='text-align: center; margin-top:-0.7em;'><strong style='font-size:3.5em;'>" + emailInboxString + "</strong></p>";

            //if (timeSpentInOutlook > 1)
            //{
            //    html += "<p style='text-align: center; margin-top:-0.7em;'>time spent in Outlook: " + Math.Round(timeSpentInOutlook, 0) + "min</p>";
            //}

            return html;
        }
    }
}
