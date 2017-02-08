// Created by André Meyer at MSR
// Created: 2016-01-13
// 
// Licensed under the MIT License.


using Shared;

namespace Retrospection.Feedback
{
    public class FeedbackBox : BaseVisualization, IVisualization
    {
        public FeedbackBox(VisType type)
        {
            Title = "Got feedback?";
            IsEnabled = Settings.IsFeedbackEnabled;
            Order = 22; //todo: handle by user
            Size = VisSize.Small;
            Type = type;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            /////////////////////
            // CSS
            /////////////////////
            html += "<style type='text/css'>";
            html += ".button { padding:0.3125em; background-color:white; border:1px solid " + Settings.RetrospectionColorHex + "; color:" + Settings.RetrospectionColorHex + "; text-decoration:none; margin:0 auto; display: block; }";
            html += ".button:hover { background-color:" + Settings.RetrospectionColorHex + "; border:1px solid " + Settings.RetrospectionColorHex + "; color:white; cursor: pointer; cursor: hand; }";
            html += "</style>";

            /////////////////////
            // HTML
            /////////////////////
            html += "<div style='width:100%; height:100%;'>";
            html += "<p style='text-align: center;' onclick=\"window.external.JS_SendFeedback()\">We would really appreciate your feedback and suggestions!</p>";
            html += "<button class='button' onclick=\"window.external.JS_SendFeedback()\">Drop us a note</button>";
            html += "</div>";

            return html;
        }
    }
}