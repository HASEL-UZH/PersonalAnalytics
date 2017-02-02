// Created by André Meyer at MSR
// Created: 2015-12-01
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Shared;
using Shared.Helpers;
using UserInputTracker.Data;
using Shared.Data;

namespace UserInputTracker.Visualizations
{
    internal class DayUserInputLineChart : BaseVisualization, IVisualization
    {
        private readonly DateTimeOffset _date;

        public DayUserInputLineChart(DateTimeOffset date)
        {
            this._date = date;

            Title = "Active Times";
            IsEnabled = true; //todo: handle by user
            Order = 18; //todo: handle by user
            Size = VisSize.Square;
            Type = VisType.Day;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            /////////////////////
            // fetch data sets
            /////////////////////
            var chartQueryResultsLocal = Queries.GetUserInputTimelineData_v2(_date);

            // if no user input data available, try to get it from the old data (V1 user input tracker)
            // TODO: remove at some point (when no one is using the old user-input tracker anymore)
            if (chartQueryResultsLocal.Sum(i => i.Value) == 0)
            {
                chartQueryResultsLocal = Queries.GetUserInputTimelineData_v1(_date);
            }

            // 3 is the minimum number of input-data-items - else, it makes no sense to show a visualization
            if (chartQueryResultsLocal.Count < 3) 
            {
                html += VisHelper.NotEnoughData(Dict.NotEnoughData);
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
            html += "<p style='text-align: center; font-size: 0.66em;'>Hint: Visualizes your active times, based on your keyboard and mouse input.</p>";


            /////////////////////
            // JS
            /////////////////////
            var ticks = CalculateLineChartAxisTicks(_date);
            var timeAxis = chartQueryResultsLocal.Aggregate("", (current, a) => current + (DateTimeHelper.JavascriptTimestampFromDateTime(a.Key) + ", ")).Trim().TrimEnd(',');
            var userInputFormattedData = chartQueryResultsLocal.Aggregate("", (current, p) => current + (p.Value + ", ")).Trim().TrimEnd(',');
            var maxUserInput = chartQueryResultsLocal.Max(i => i.Value);
            var avgUserInput = chartQueryResultsLocal.Average(i => i.Value);

            const string colors = "'User_Input_Level' : '" + Shared.Settings.RetrospectionColorHex + "'";
            var data = "x: 'timeAxis', columns: [['timeAxis', " + timeAxis + "], ['User_Input_Level', " + userInputFormattedData + " ] ], type: 'area', colors: { " + colors + " }, axis: { 'PerceivedProductivity': 'y' }";
            var grid = "y: { lines: [ { value: 0, text: 'not active' }, { value: "+ avgUserInput + ", text: 'average activity today' }, { value: "+ maxUserInput + ", text: 'max activity today' } ] } ";
            var axis = "x: { localtime: true, type: 'timeseries', tick: { values: [ " + ticks + "], format: function(x) { return formatDate(x.getHours()); }}  }, y: { show: false, min: 0 }";
            var parameters = " bindto: '#" + VisHelper.CreateChartHtmlTitle(Title) + "', data: { " + data + " }, padding: { left: 0, right: 0, bottom: -10, top: 0}, legend: { show: false }, axis: { " + axis + " }, grid: { " + grid + " }, tooltip: { show: false }, point: { show: false }";
            // padding: { left: 0, right: 0 }, 

            html += "<script type='text/javascript'>";
            html += "var formatDate = function(hours) { var suffix = 'AM'; if (hours >= 12) { suffix = 'PM'; hours = hours - 12; } if (hours == 0) { hours = 12; } if (hours < 10) return '0' + hours + ' ' + suffix; else return hours + ' ' + suffix; };";
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
