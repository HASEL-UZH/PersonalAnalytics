// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2016-06-20
// 
// Licensed under the MIT License.

using MsOfficeTracker.Data;
using Shared;
using Shared.Helpers;
using System;

namespace MsOfficeTracker.Visualizations
{
    public class DayEmailsTable : BaseVisualization, IVisualization
    {
        private readonly DateTimeOffset _date;

        public DayEmailsTable(DateTimeOffset date)
        {
            this._date = date;

            Title = "Email Stats (with Averages)";
            IsEnabled = true; //todo: handle by user
            Order = 6; //todo: handle by user
            Size = VisSize.Square;
            Type = VisType.Day;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            /////////////////////
            // fetch data sets
            /////////////////////

            // get the latest stored email entry
            var emailsSnapshotResult = Queries.GetEmailsSnapshot(_date.Date);
            var inbox = emailsSnapshotResult.Item2;
            var inboxUnread = emailsSnapshotResult.Item3;
            var sent = emailsSnapshotResult.Item4;
            var received = emailsSnapshotResult.Item5;
            var receivedUnread = emailsSnapshotResult.Item6;

            var isToday = (_date.Date == DateTime.Now.Date);
            var lastUpdatedMinsAgo = Math.Abs((DateTime.Now - emailsSnapshotResult.Item1).TotalMinutes);

            // if database entry is outdated or not there, create a live API call and override entries
            if (emailsSnapshotResult.Item1 == DateTime.MinValue || // no emails stored yet
                (isToday && lastUpdatedMinsAgo > Settings.SaveEmailCountsInterval_InMinutes)) // request is for today and saved results are too old // could not fetch sent emails
            {
                // create and save a new email snapshot (inbox, sent, received)
                var res = Queries.CreateEmailsSnapshot(_date.Date, false);
                inbox = res.Item1;
                inboxUnread = res.Item2;
                sent = res.Item3;
                received = res.Item4;
                receivedUnread = res.Item5;
            }

            // error (only if no data at all)
            if (sent < 0 && (received < 0 || receivedUnread < 0) && inbox < 0 && inboxUnread < 0)
            {
                return VisHelper.NotEnoughData(Dict.NotEnoughData);
            }

            // get averages over last 2 months
            var averagesSnapshot = Queries.GetEmailsSnapshotAverages(60);


            /////////////////////
            // HTML
            /////////////////////

            html += "<table>";
            if (sent > Settings.NoValueDefault) html += "<tr><td><strong style='font-size:2.5em; color:#007acc;'>" + sent + "</strong></td><td>emails sent" + FormatAverage(sent, averagesSnapshot.Item3) + "</td></tr>";
            if (received > Settings.NoValueDefault && receivedUnread > Settings.NoValueDefault) html += "<tr><td><strong style='font-size:2.5em; color:#007acc;'>" + (received - receivedUnread) + "</strong></td><td>emails received that are read" + FormatAverage((received - receivedUnread), averagesSnapshot.Item4 - averagesSnapshot.Item5) + "</td></tr>";
            if (received > Settings.NoValueDefault && receivedUnread > Settings.NoValueDefault) html += "<tr><td><strong style='font-size:2.5em; color:#007acc;'>" + receivedUnread + "</strong></td><td>emails received that are unread" + FormatAverage(receivedUnread, averagesSnapshot.Item5) + "</td></tr>";
            if (received > Settings.NoValueDefault && receivedUnread == Settings.NoValueDefault) html += "<tr><td><strong style='font-size:2.5em; color:#007acc;'>" + received + "</strong></td><td>emails received" + FormatAverage(received, averagesSnapshot.Item4) + "</td></tr>";
            if (inbox > Settings.NoValueDefault) html += "<tr><td><strong style='font-size:2.5em; color:#007acc;'>" + inbox + "</strong></td><td>emails in your inbox" + FormatAverage(inbox, averagesSnapshot.Item1) + "</td></tr>";
            if (inboxUnread > Settings.NoValueDefault) html += "<tr><td><strong style='font-size:2.5em; color:#007acc;'>" + inboxUnread + "</strong></td><td>unread emails in your inbox" + FormatAverage(inboxUnread, averagesSnapshot.Item2) + "</td></tr>";
            html += "</table>";

            return html;
        }

        private string FormatAverage(long currentValue, double average)
        {
            string averageCorr = "  (≈) ";
            var diff = (currentValue - average);
            if (diff >= 1) // more than average
            {
                averageCorr = "  (▲+" + Math.Abs(Math.Round(diff, 0)) + ")";
            }
            else if (diff <= -1) // less than average
            {
                averageCorr = "  (▼-" + Math.Abs(Math.Round(diff, 0)) + ")";
            }
            return averageCorr;
        }
    }
}
