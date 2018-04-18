// Created by André Meyer at University of Zurich
// Created: 2018-02-07
// 
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;
using Shared;
using Shared.Helpers;
using WindowsActivityTracker.Data;

namespace WindowsActivityTracker.Visualizations
{
    internal class WeekWorkTimeBarChart : BaseVisualization, IVisualization
    {
        private readonly DateTimeOffset _date;

        public WeekWorkTimeBarChart(DateTimeOffset date)
        {
            this._date = date;

            Title = "Time spent at Work and the Computer";
            IsEnabled = true; //todo: handle by user
            Order = 1; //todo: handle by user
            Size = VisSize.Square;
            Type = VisType.Week;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            /////////////////////
            // fetch data sets
            /////////////////////
            var workTimeData = GetWorkTimeData();

            if (workTimeData.Where(i => i.Value.Item1 > 0).Count() < 1)
            {
                return VisHelper.NotEnoughData();
            }

            /////////////////////
            // HTML
            /////////////////////
            html += "<div id='" + VisHelper.CreateChartHtmlTitle(Title) + "' style='height:85%;' align='center'></div>";


            /////////////////////
            // JS
            /////////////////////
            var totalTimespentData = workTimeData.Aggregate("", (current, p) => current + (Math.Round(p.Value.Item1, 1) + ", ")).Trim().TrimEnd(',');
            var timeSpentActiveData = workTimeData.Aggregate("", (current, p) => current + (Math.Round(p.Value.Item2, 1) + ", ")).Trim().TrimEnd(',');
            var formattedXAxis = workTimeData.Aggregate("", (current, p) => current + ("'" + DateTimeHelper.GetShortestDayName(p.Key) + "', ")).Trim().TrimEnd(',');

            var totalTimespentName = "at work in total (hours)";
            var timeSpentActiveName = "active at the computer (hours)";

            var data = "columns: [ ['" + totalTimespentName + "', " + totalTimespentData + "], ['" + timeSpentActiveName + "', " + timeSpentActiveData + "] ], type: 'bar' ";
            var bar = "width: { ratio: 0.5 }";
            var colors = "'" + totalTimespentName + "' : '" + Shared.Settings.RetrospectionColorHex + "', '" + timeSpentActiveName + "' : '" + Shared.Settings.DarkGrayColorHex + "'";
            var axis = "x: { type: 'category', categories: [ " + formattedXAxis + " ] }, y: { max: " + workTimeData.Max(i => i.Value.Item1)  +  " }";
            var parameters = " bindto: '#" + VisHelper.CreateChartHtmlTitle(Title) + "', data: { " + data + " }, bar: { " + bar + " }, colors: { " + colors + " }, axis: { " + axis + " }, padding: { left: 20, right: 0, bottom: 0, top: 0}, grid: { y: { show: true } }, legend: { show: true } ";

            html += "<script type='text/javascript'>";
            html += "var " + VisHelper.CreateChartHtmlTitle(Title) + " = c3.generate({ " + parameters + " });";
            html += "</script>";

            return html;
        }

        private Dictionary<DateTimeOffset, Tuple<double, double>> GetWorkTimeData()
        {
            var first = DateTimeHelper.GetFirstDayOfWeek_Iso8801(_date);
            var last = DateTimeHelper.GetLastDayOfWeek_Iso8801(_date);

            var dict = new Dictionary<DateTimeOffset, Tuple<double, double>>();

            while (first <= last)
            {
                if (first.Date <= DateTime.Now.Date)
                {
                    var tuple = Queries.GetWorkTimeDetails(first);
                    dict.Add(first, tuple);
                }
                first = first.AddDays(1);
            }

            return dict;
        }
    }
}
