// Created by André Meyer at MSR
// Created: 2016-01-04
// 
// Licensed under the MIT License.

using System;
using System.Linq;
using Shared;
using UserEfficiencyTracker.Data;
using Shared.Helpers;
using System.Collections.Generic;
using Shared.Data;

namespace UserEfficiencyTracker.Visualizations
{
    internal class DayProductivityTimeLine : BaseVisualization, IVisualization
    {
        private readonly DateTimeOffset _date;

        public DayProductivityTimeLine(DateTimeOffset date)
        {
            this._date = date;

            Title = "Perceived Productivity over the Day";
            IsEnabled = true; //todo: handle by user
            Order = 8; //todo: handle by user
            Size = VisSize.Square;
            Type = VisType.Day;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            /////////////////////
            // fetch data sets
            /////////////////////
            var chartQueryResultsLocal = Queries.GetUserProductivityTimelineData(_date, VisType.Day);

            if (chartQueryResultsLocal.Count < 3) // 3 is the minimum number of input-data-items
            {
                html += VisHelper.NotEnoughData("It is not possible to give you insights into your productivity as you didn't fill out the pop-up often enough. Try to fill it out at least 3 times per day.");
                return html;
            }


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
            html += "<div id='" + VisHelper.CreateChartHtmlTitle(Title) + "' style='height:75%;' align='center'></div>";
            html += "<p style='text-align: center; font-size: 0.66em;'>Hint: Interpolates your perceived productivity, based on your pop-up responses.</p>";
            

            /////////////////////
            // JS
            /////////////////////
            var ticks = CalculateLineChartAxisTicks(_date);
            var timeAxis = chartQueryResultsLocal.Aggregate("", (current, a) => current + (DateTimeHelper.JavascriptTimestampFromDateTime(a.Item1) + ", ")).Trim().TrimEnd(',');
            var productivityFormattedData = chartQueryResultsLocal.Aggregate("", (current, p) => current + (p.Item2 + ", ")).Trim().TrimEnd(',');

            const string colors = "'User_Input_Level' : '" + Shared.Settings.RetrospectionColorHex + "'";
            var data = "x: 'timeAxis', columns: [['timeAxis', " + timeAxis + "], ['Productivity', " + productivityFormattedData + " ] ], type: 'line', colors: { " + colors + " }, axis: { 'PerceivedProductivity': 'y' } "; // type options: spline, step, line
            var grid = "y: { lines: [ { value: 1, text: 'not at all productive' }, { value: 4, text: 'moderately productive' }, { value: 7, text: 'very productive' } ] } ";
            var axis = "x: { localtime: true, type: 'timeseries', tick: { values: [ " + ticks + "], format: function(x) { return formatDate(x.getHours()); }}  }, y: { min: 1, max: 7 }"; // show: false, 
            var tooltip = "show: true, format: { title: function(d) { return 'Pop-Up answered: ' + formatTime(d.getHours(),d.getMinutes()); }}";
            var parameters = " bindto: '#" + VisHelper.CreateChartHtmlTitle(Title) + "', data: { " + data + " }, padding: { left: 15, right: 0, bottom: -10, top: 0}, legend: { show: false }, axis: { " + axis + " }, grid: { " + grid + " }, tooltip: { " + tooltip + " }, point: { show: true }";


            html += "<script type='text/javascript'>";
            html += "var formatDate = function(hours) { var suffix = 'AM'; if (hours >= 12) { suffix = 'PM'; hours = hours - 12; } if (hours == 0) { hours = 12; } if (hours < 10) return '0' + hours + ' ' + suffix; else return hours + ' ' + suffix; };";
            html += "var formatTime = function(hours, minutes) { var minFormatted = minutes; if (minFormatted < 10) minFormatted = '0' + minFormatted; var suffix = 'AM'; if (hours >= 12) { suffix = 'PM'; hours = hours - 12; } if (hours == 0) { hours = 12; } if (hours < 10) return '0' + hours + ':' + minFormatted + ' ' + suffix; else return hours + ':' + minFormatted + ' ' + suffix; };";
            html += "var " + VisHelper.CreateChartHtmlTitle(Title) + " = c3.generate({ " + parameters + " });"; // return x.getHours() + ':' + x.getMinutes();
            html += "</script>";

            return html;
        }

        /// <summary>
        /// Creates a list of one-hour axis times
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private static string CalculateLineChartAxisTicks(DateTimeOffset date)
        {
            var dict = new Dictionary<DateTime, int>();
            VisHelper.PrepareTimeAxis(date, dict, 60);

            return dict.Aggregate("", (current, a) => current + (DateTimeHelper.JavascriptTimestampFromDateTime(a.Key) + ", ")).Trim().TrimEnd(',');
        }
    }
}
