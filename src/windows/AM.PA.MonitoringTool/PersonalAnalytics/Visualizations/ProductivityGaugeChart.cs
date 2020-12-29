// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Documents;
using Shared;
using Shared.Data;

namespace PersonalAnalytics.Visualizations
{
    internal class ProductivityGaugeChart : IChart
    {
        private readonly DateTimeOffset _date;

        public ProductivityGaugeChart(DateTimeOffset date)
        {
            _date = date;
        }

        public string GetHtml()
        {
            var html = String.Empty;
            var productivityGaugeData = new List<ProductivityTimeDto>();

            // fetch data sets
            var productivityGaugeDataLocal = Database.GetInstance().GetUserProductivityData(_date, false);

            // merge with remote data if necessary
            productivityGaugeData = RemoteDataHandler.VisualizeWithRemoteData()
                ? RemoteDataHandler.MergeProductivityData(productivityGaugeDataLocal, Database.GetInstanceRemote().GetUserProductivityData(_date, false))
                : productivityGaugeDataLocal;

            html += VisHelper.ChartTitle("Your Perceived Productivity");

            if (productivityGaugeData.Count == 0)
            {
                html += VisHelper.NotEnoughData(Dict.NotEnoughDataMiniSurvey);
                return html;
            }

            // Calculate Productivityvalue
            var productivityValue = CalculateWeightedProductivityValue(productivityGaugeData);

            /////////////////////
            // Some strings for the attributes
            /////////////////////
            const string gaugeChartName = "gaugeChartName";
            const int height = 100;

            /////////////////////
            // CSS
            /////////////////////
            html += "<style type='text/css'>";
            html += ".c3-gauge-value { fill: white; visibility:hidden; }"; // hack to hide the gauge value
            html += "</style>";

            /////////////////////
            // HTML
            /////////////////////
            //html += "<p style='text-align: center;'>Average productivity based on your manual selection in the mini-surveys<br />(1 = very unproductive, 7 = very productive).</p>";
            html += "<div id='" + gaugeChartName + "' align='center'></div>";

            /////////////////////
            // JS
            /////////////////////

            var productivityValueString = Math.Round(productivityValue, 1).ToString().Replace(',', '.');
            var data = "columns: [ ['PerceivedProductivity', " + productivityValueString + "] ], type: 'gauge'";
            const string gauge = " label : { show: false }, min: 1, max: 7, width: 36";
            const string color = "pattern: ['#FF0000', '#F97600', '#F6C600', '#60B044'], threshold: { unit: 'value', max: 7, values: [1, 2, 4, 6] }";
            var size = "height: " + height;
            var parameters = " bindto: '#" + gaugeChartName + "', data: { " + data + " }, gauge: { " + gauge + " }, color: { " + color + " }, size: { " + size + " }";

            html += "<script type='text/javascript'>";
            html += "var " + gaugeChartName + " = c3.generate({ " + parameters + " });";
            html += "</script>";

            return html;
        }

        private double CalculateWeightedProductivityValue(IEnumerable<ProductivityTimeDto> productivityGaugeData)
        {
            long totalTime = 0;
            long totalSum = 0;
            var previousStartWorkingTime =
                Helpers.DateTimeFromJavascriptTimestamp(
                    Helpers.JavascriptTimestampFromDateTime(Database.GetInstance().GetUserWorkStart(_date)));

            foreach (var item in productivityGaugeData)
            {
                var endTime = Helpers.DateTimeFromJavascriptTimestamp(item.Time);
                var duration = (endTime - previousStartWorkingTime).Ticks;

                totalTime += duration;
                totalSum += duration*item.UserProductvity;

                //Console.WriteLine("Start: {0} End: {1} Duration: {2} Productivity: {3} totalTime: {4}, totalSum: {5}", previousStartWorkingTime, endTime, duration, item.UserProductvity, totalTime, totalSum);
                previousStartWorkingTime = endTime;
            }

            var productivityValue = (double) totalSum/totalTime;
            return productivityValue;
        }
    }
}
