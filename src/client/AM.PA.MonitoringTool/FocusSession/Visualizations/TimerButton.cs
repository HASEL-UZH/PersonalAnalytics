// Created by Philip Hofmann (philip.hofmann@uzh.ch) from the University of Zurich
// Created: 2020-02-11
// 
// Licensed under the MIT License.

using Shared;

namespace FocusSession.Visualizations
{
    internal class TimerButton : BaseVisualization, IVisualization
    {
        public TimerButton()
        {
            Title = "FocusTimer";
            IsEnabled = true; //todo: handle by user
            Order = 5; //todo: handle by user (its the order by whihc those will be displayed, 1 means left top corner)
            Size = VisSize.Square;
            Type = VisType.Day;
        }
        public override string GetHtml()
        {
            var html = string.Empty;

            /////////////////////
            // fetch data sets
            /////////////////////

            /////////////////////
            // HTML
            /////////////////////

            html += "<table>";
            html += "< button type = \"button\" > Start Timer </ button >";
            //if (sent > Settings.NoValueDefault) html += "<tr><td><strong style='font-size:2.5em; color:#007acc;'>" + sent + "</strong></td><td>emails sent" + FormatAverage(sent, averagesSnapshot.Item3) + "</td></tr>";
            //if (received > Settings.NoValueDefault && receivedUnread > Settings.NoValueDefault) html += "<tr><td><strong style='font-size:2.5em; color:#007acc;'>" + (received - receivedUnread) + "</strong></td><td>emails received that are read" + FormatAverage((received - receivedUnread), averagesSnapshot.Item4 - averagesSnapshot.Item5) + "</td></tr>";
            //if (received > Settings.NoValueDefault && receivedUnread > Settings.NoValueDefault) html += "<tr><td><strong style='font-size:2.5em; color:#007acc;'>" + receivedUnread + "</strong></td><td>emails received that are unread" + FormatAverage(receivedUnread, averagesSnapshot.Item5) + "</td></tr>";
            //if (received > Settings.NoValueDefault && receivedUnread == Settings.NoValueDefault) html += "<tr><td><strong style='font-size:2.5em; color:#007acc;'>" + received + "</strong></td><td>emails received" + FormatAverage(received, averagesSnapshot.Item4) + "</td></tr>";
            //if (inbox > Settings.NoValueDefault) html += "<tr><td><strong style='font-size:2.5em; color:#007acc;'>" + inbox + "</strong></td><td>emails in your inbox" + FormatAverage(inbox, averagesSnapshot.Item1) + "</td></tr>";
            //if (inboxUnread > Settings.NoValueDefault) html += "<tr><td><strong style='font-size:2.5em; color:#007acc;'>" + inboxUnread + "</strong></td><td>unread emails in your inbox" + FormatAverage(inboxUnread, averagesSnapshot.Item2) + "</td></tr>";
            html += "</table>";

            return html;
        }
    }
}
