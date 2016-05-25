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
    internal class DayEmailsSent : BaseVisualization, IVisualization
    {
        private readonly DateTimeOffset _date;

        public DayEmailsSent(DateTimeOffset date)
        {
            this._date = date;

            Title = "Total Number<br />of Emails Sent";
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
            var emailsSentResult = Queries.GetSentEmails(_date);
            var emailsSent = emailsSentResult.Item2;

            var isToday = (_date.Date == DateTime.Now.Date);
            var lastUpdatedMinsAgo = Math.Abs((DateTime.Now - emailsSentResult.Item1).TotalMinutes);

            // if database entry is outdated or not there, create a live API call and override entries
            if (emailsSentResult.Item1 == DateTime.MinValue || // no emails stored yet
                (isToday && lastUpdatedMinsAgo > Settings.SaveEmailCountsIntervalInMinutes) || // request is for today and saved results are too old
                (emailsSent == -1 )) // could not fetch sent emails
            {
                // create and save a new email snapshot (inbox, sent, received)
                var res = Queries.CreateEmailsSnapshot(_date.Date, false);
                emailsSent = res.Item1;
            }

            // error (only if no data at all)
            if (emailsSent == -1)
            {
                return VisHelper.NotEnoughData(Dict.NotEnoughData);
            }

            // no emails sent/received
            if (emailsSent == 0)
            {
                return VisHelper.NotEnoughData("You didn't send any emails that day.");
            }

            // as a goodie get this too :)
            //var timeSpentInOutlook = Queries.TimeSpentInOutlook(_date);


            /////////////////////
            // HTML
            /////////////////////

            var emailsSentString = (emailsSent == -1) ? "?" : emailsSent.ToString(CultureInfo.InvariantCulture);

            html += "<p style='text-align: center; margin-top:-0.7em;'><strong style='font-size:3.5em;'>" + emailsSentString + "</strong></p>";

            //if (timeSpentInOutlook > 1)
            //{
            //    html += "<p style='text-align: center; margin-top:-0.7em;'>time spent in Outlook: " + Math.Round(timeSpentInOutlook, 0) + "min</p>";
            //}

            return html;
        }
    }
}
