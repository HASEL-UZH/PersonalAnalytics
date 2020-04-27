
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
        public TimerButton(System.DateTimeOffset date)
        {
            this._date = date;
            Title = "FocusTimer";
            IsEnabled = true; //todo: handle by user
            Order = 13; //todo: handle by user (its the order by whihc those will be displayed, 1 means left top corner)
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
            html += "<table>";
            html += "<p style='text-align: center;'>Click on the Buttons to manually manage your Focus Session</p>";
            html += "<button class='button' onclick=\"window.external.JS_StartFocusSession\" style=\"display:inline-block;margin-right:20px;margin-left:50px;\">Start Focus Session</button>";
            html += "<button class='button' onclick=\"window.external.JS_StopFocusSession\" style=\"display:inline-block;\">Stop Focus Session</button>";
            html += "<p style='text-align: center;'>You can also declare the amount of time (minutes) that a focus session shall run and then click on the countdown button</p>";
            html += "<input type=\"number\" style=\"display:inline-block;\">";
            html += "<button class='button' onclick=\"window.external.JS_FocusSessionCustomTimer()\" style=\"display:inline-block;\">Submit Counter</button>";

            //html += " < input type = \"number\" id = \"ctime\" name = \"ctime\" > ";
            // why not giving predefined time buttons instead of let them freely insert times? This way we can also extend the context menu with options, like 15min, 30min, 45min. Then they can choose it withouth having retrospection open.
            // then we show timer functionality. We can differentiate between people wanting a set time or to manually (selbstkontrolle) start and stop focus sessions.
            html += "<p style='text-align: center;'>Or use these predefined timers.</p>";
            html += "<button class='button' onclick=\"window.external.JS_FocusSessionCountdown\" style=\"display:inline-block;\">Pomodo Timer</button>";
            html += "</table>";

            return html;
        }
    }
}
