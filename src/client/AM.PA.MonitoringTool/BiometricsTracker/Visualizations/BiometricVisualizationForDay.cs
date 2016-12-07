// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using BiometricsTracker.Data;
using Shared;
using System;

namespace BiometricsTracker.Visualizations
{
    internal class BiometricVisualizationForDay : BaseVisualization, IVisualization
    {
        private DateTimeOffset date;

        public BiometricVisualizationForDay(DateTimeOffset date)
        {
            this.date = date;

            Title = "Averaged Heartrate over the Day";
            IsEnabled = true;
            Order = 10;
            Size = VisSize.Square;
            Type = VisType.Day;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

           
            /////////////////////
            // CSS
            /////////////////////
            html += "<style type='text/css'>";
            html += ".c3-line { stroke-width: 2px; }";
            html += ".c3-grid text, c3.grid line { fill: gray; }";
            html += "</style>";


            /////////////////////
            // HTML
            /////////////////////
            html += "<p style='text-align: center; font-size: 5em;'>" + DatabaseConnector.GetAverageHeartrate(date, VisType.Day) + "</p>";


            
            return html;
        }
    }
}
