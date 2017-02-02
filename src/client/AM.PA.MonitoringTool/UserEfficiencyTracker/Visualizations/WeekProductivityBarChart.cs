// Created by André Meyer at MSR
// Created: 2016-01-06
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
    internal class WeekProductivityBarChart : BaseVisualization, IVisualization
    {
        private readonly DateTimeOffset _date;

        public WeekProductivityBarChart(DateTimeOffset date)
        {
            this._date = date;

            Title = "Average Perceived Productivity during the Week";
            IsEnabled = true; //todo: handle by user
            Order = 2; //todo: handle by user
            Size = VisSize.Square;
            Type = VisType.Week;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            /////////////////////
            // fetch data sets
            /////////////////////
            var averageProductivityPerDay = GetAverageProductivityPerDay();

            if (NotEnoughDataSets(averageProductivityPerDay))
            {
                html += VisHelper.NotEnoughData("It is not possible to give you insights into your productivity as you didn't fill out the pop-up often enough or didn't work. Try to fill it out at least 3 times per day.");
                return html;
            }

            /////////////////////
            // HTML
            /////////////////////
            html += "<div id='" + VisHelper.CreateChartHtmlTitle(Title) + "' style='height:80%;' align='center'></div>";


            /////////////////////
            // JS
            /////////////////////
            var productivityFormattedData = averageProductivityPerDay.Aggregate("", (current, p) => current + (Math.Round(p.Value, 1) + ", ")).Trim().TrimEnd(',');
            var formattedXAxis = averageProductivityPerDay.Aggregate("", (current, p) => current + ("'" + DateTimeHelper.GetShortestDayName(p.Key) + "', ")).Trim().TrimEnd(',');

            var data = "columns: [ ['Average Productivity', " + productivityFormattedData + "] ], type: 'bar' ";
            var bar = "width: { ratio: 0.5 }";
            var colors = "'Average Productivity' : '" + Shared.Settings.RetrospectionColorHex + "'";
            var axis = "x: { type: 'category', categories: [ " + formattedXAxis + " ] }, y: { max: 7, min: 1, tick: { values: [ 1, 2, 3, 4, 5, 6, 7 ] } }";
            var parameters = " bindto: '#" + VisHelper.CreateChartHtmlTitle(Title) + "', data: { " + data + " }, bar: { " + bar + " }, colors: { " + colors + " }, axis: { " + axis + " }, padding: { left: 20, right: 0, bottom: 0, top: 0}, grid: { y: { show: true } }, legend: { show: false } ";


            html += "<script type='text/javascript'>";
            html += "var " + VisHelper.CreateChartHtmlTitle(Title) + " = c3.generate({ " + parameters + " });";
            html += "</script>";

            return html;
        }

        private bool NotEnoughDataSets(Dictionary<DateTimeOffset, double> dict)
        {
            var hasData = false;

            foreach (var item in dict)
            {
                if (item.Value > 0)
                {
                    hasData = true;
                    break;
                }
            }

            return !hasData;
        }

        private Dictionary<DateTimeOffset, double> GetAverageProductivityPerDay()
        {
            var first = DateTimeHelper.GetFirstDayOfWeek_Iso8801(_date);
            var last = DateTimeHelper.GetLastDayOfWeek_Iso8801(_date);
                
            var dict = new Dictionary<DateTimeOffset, double>();

            while (first <= last)
            {
                var list = Queries.GetUserProductivityTimelineData(first, VisType.Day);
                var weightedAverage = CalculateWeightedProductivityValue(list);
                dict.Add(first, weightedAverage);
                first = first.AddDays(1);
            }

            return dict;
        }

        private double CalculateWeightedProductivityValue(List<Tuple<DateTime, int>> productivityGaugeData)
        {
            long totalTime = 0;
            long totalSum = 0;

            if (productivityGaugeData.Count == 0) return 0;
            var previousStartWorkingTime = productivityGaugeData[0].Item1;

            for (int i = 1 ; i < productivityGaugeData.Count; i++)
            {
                var thisItem = productivityGaugeData[i];
                var endTime = thisItem.Item1;

                // skip if didn't work
                if (thisItem.Item2 > 7 || thisItem.Item2 < 1) previousStartWorkingTime = endTime;

                // add duration
                var duration = (endTime - previousStartWorkingTime).Ticks;
                totalTime += duration;
                totalSum += duration * thisItem.Item2;

                //Console.WriteLine("Start: {0} End: {1} Duration: {2} Productivity: {3} totalTime: {4}, totalSum: {5}", previousStartWorkingTime, endTime, duration, thisItem.Item2, totalTime, totalSum);
                previousStartWorkingTime = endTime;
            }

            var productivityValue = (double) totalSum / totalTime;
            return productivityValue;
        }

    }
}
