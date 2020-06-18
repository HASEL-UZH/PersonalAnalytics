
// Created by Philip Hofmann (philip.hofmann@uzh.ch) from the University of Zurich
// Created: 2020-02-11
// 
// Licensed under the MIT License.

using Shared;

namespace FocusSession.Visualizations
{
    internal class TimerButton : BaseVisualization, IVisualization
    {
        private readonly System.DateTimeOffset _date;
        // get the amount of time total focused for today
        private System.TimeSpan totalDay = Data.Queries.GetFocusTimeFromDay(System.DateTime.Now);

        // get the amount of time total focused for this week
        private System.TimeSpan totalWeek = Data.Queries.GetFocusTimeFromDay(Controls.Timer.StartOfWeek(System.DayOfWeek.Monday));

        // get the amount of time total focused for this month
        private System.TimeSpan totalMonth = Data.Queries.GetFocusTimeFromDay(new System.DateTime(System.DateTime.Now.Year, System.DateTime.Now.Month, 1));

        // get the total amount of emails received during a session
        private int emailMessages = Data.Queries.GetEmailMessagesReceived();

        // get the total amount of slack received during a session
        private int slackMessages = Data.Queries.GetSlackMessagesReceived();

        // get the total amount of emails replied during a session
        private int emailReplied = Data.Queries.GetEmailMessagesReplied();

        // get the total amount of slack replied during a session
        private int slackReplied = Data.Queries.GetSlackMessagesReplied();

        // get the total amount of flaggers displayed
        private int flaggerDisplayed = Data.Queries.GetFlaggerDisplayed();

        // get the total amount of sessions
        private int sessionsRun = Data.Queries.GetSessionsRun();

        public TimerButton(System.DateTimeOffset date)
        {
            this._date = date;
            Title = "FocusTimer";
            IsEnabled = true; //todo: handle by user
            Order = 0; //todo: handle by user (its the order by whihc those will be displayed, 1 means left top corner)
            Size = VisSize.Square;
            Type = VisType.Day;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            /////////////////////
            // fetch data sets
            /////////////////////
            ///


            /////////////////////
            // CSS
            /////////////////////
            html += "<style type='text/css'>";
            html += ".button { padding:0.3125em; background-color:white; border:1px solid " + Shared.Settings.RetrospectionColorHex + "; color:" + Shared.Settings.RetrospectionColorHex + "; text-decoration:none; margin:0 auto; display: block; }";
            html += ".button:hover { background-color:" + Shared.Settings.RetrospectionColorHex + "; border:1px solid " + Shared.Settings.RetrospectionColorHex + "; color:white; cursor: pointer; cursor: hand; }";
            html += "</style>";

            /////////////////////
            // HTML
            /////////////////////
            ///
            // TODO maybe show interesting stats here like total amount of time focussed with FocusTimer; total amount of emails answered 
            html += "<table>";
            html += "<p style='text-align: left; font-size: medium;'>Total Amount of sessions run: " + sessionsRun + "</p>";
            html += "<p style='text-align: left; font-size: medium; margin: 0rem;'>Total time focused this day: " + totalDay.Hours + " hours and " + totalDay.Minutes + " minutes</p>";
            html += "<p style='text-align: left; font-size: medium; margin: 0rem;'>Total time focused this week: " + totalWeek.Hours + " hours and " + totalWeek.Minutes + " minutes</p>";
            html += "<p style='text-align: left; font-size: medium; margin: 0rem;'>Total time focused this month: " + totalMonth.Hours + " hours and " + totalMonth.Minutes + " minutes</p>";
            html += "<p style='text-align: left; font-size: medium; margin-bottom : 0rem;'>Total Emails received during sessions: " + emailMessages + "</p>";
            html += "<p style='text-align: left; font-size: medium; margin-top: 0rem;'>Total Emails replied during sessions: " + emailReplied + "</p>";
            html += "<p style='text-align: left; font-size: medium; margin-bottom : 0rem;'>Total Slack Messages received during sessions: " + slackMessages + "</p>";
            html += "<p style='text-align: left; font-size: medium; margin-top: 0rem;'>Total Slack Messages replied during sessions: " + slackReplied + "</p>";
            html += "<p style='text-align: left; font-size: medium;'>Total Flagger displayed during sessions: " + flaggerDisplayed + "</p>";
            html += "</table>";

            return html;
        }
    }
}
