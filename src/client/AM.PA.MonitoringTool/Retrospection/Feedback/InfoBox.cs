// Created by André Meyer (ameyer@ifi.uzh.ch) at MSR
// Created: 2016-01-13
// 
// Licensed under the MIT License.


using System.Windows.Forms;
using Shared;

namespace Retrospection.Feedback
{
    public class InfoBox : BaseVisualization, IVisualization
    {
        private readonly string _message;
        private readonly bool _showFeedbackButton;

        public InfoBox(VisType type, string message, string title = "Information", bool showFeedbackButton = true)
        {
            Title = title;
            IsEnabled = Shared.Settings.IsFeedbackEnabled;
            Order = 22; //todo: handle by user
            Size = VisSize.Small;
            Type = type;

            _message = message;
            _showFeedbackButton = showFeedbackButton;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            /////////////////////
            // CSS
            /////////////////////
            if (_showFeedbackButton)
            {
                html += "<style type='text/css'>";
                html += ".button { padding:0.3125em; background-color:white; border:1px solid " + Shared.Settings.RetrospectionColorHex + "; color:" + Shared.Settings.RetrospectionColorHex + "; text-decoration:none; margin:0 auto; display: block; }";
                html += ".button:hover { background-color:" + Shared.Settings.RetrospectionColorHex + "; border:1px solid " + Shared.Settings.RetrospectionColorHex + "; color:white; cursor: pointer; cursor: hand; }";
                html += "</style>";
            }

            /////////////////////
            // HTML
            /////////////////////
            html += "<div style='width:100%; height:100%;'>";
            html += "<p style='text-align: center;' onclick=\"window.external.JS_SendFeedback()\">" + _message + "</p>";
            if (_showFeedbackButton) html += "<button class='button' onclick=\"window.external.JS_SendFeedback()\">Send an Email</button>";
            html += "</div>";

            return html;
        }
    }
}