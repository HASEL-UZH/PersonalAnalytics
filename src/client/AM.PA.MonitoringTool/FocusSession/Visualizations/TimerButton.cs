﻿// Created by Philip Hofmann (philip.hofmann@uzh.ch) from the University of Zurich
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
            ///
            // take the feedback helper as a reference. There is a visual window "got feedback?" that has a button with functionality
            // Retrospection -> FeedbackBox -> Retrospection/ObjectForScriptingHelper

            html += "<table>";
            html += "<p style='text-align: center;'>Click on the Button to start a Focus Session</p>";
            html += "<button class='button' onclick=\"Test()\">Focus Session</button>";

            html += "</table>";

            return html;
        }
    }
}