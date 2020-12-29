// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using Shared;
using Shared.Data;

namespace PersonalAnalytics.Visualizations
{
    internal class ActivityPieChart : IChart
    {
        private readonly DateTimeOffset _date;

        public ActivityPieChart(DateTimeOffset date)
        {
            this._date = date;
        }

        public string GetHtml()
        {
            var html = string.Empty;
            var chartQueryPieChartData = new Dictionary<string, long>();
            var chartQueryResultsLocal = Database.GetInstance().GetActivityPieChartData(_date);

            // merge with remote data if necessary
            chartQueryPieChartData = RemoteDataHandler.VisualizeWithRemoteData()
                ? RemoteDataHandler.MergeActivityData(chartQueryResultsLocal, Database.GetInstanceRemote().GetActivityPieChartData(_date))
                : chartQueryResultsLocal;

            if (chartQueryPieChartData.Count == 0)
            {
                html += VisHelper.NotEnoughData(Dict.NotEnoughDataMiniSurvey);
                return html;
            }

            var chartUrl = PreparePieChartUrl(chartQueryPieChartData);
            var totalHoursWorked = Database.GetInstance().GetTotalHoursWorked(_date);

            html += VisHelper.ChartTitle("Distribution of the Activity on your Computer");
            html += "<p style='text-align: center;'>Total hours worked on your computer: <strong>" + totalHoursWorked + "</strong>.</p>";

            html += string.IsNullOrEmpty(chartUrl)
                ? VisHelper.NotEnoughData()
                : "<p style='text-align: center;'><img src='" + chartUrl + "'/></p>";

            return html;
        }

        /// <summary>
        /// Prepares an url to visualize the pie chart via Google Chart API
        /// </summary>
        /// <param name="chartQueryResults"></param>
        /// <returns></returns>
        private static string PreparePieChartUrl(Dictionary<string, long> chartQueryResults)
        {
            var other = 0.0;
            var per = "";
            var name = "";
            var sum = chartQueryResults.Sum(item => item.Value);

            foreach (var item in chartQueryResults)
            {
                var temp = item.Value/(sum/100.0);
                if (temp < 1.0)
                {
                    other += temp;
                    continue;
                }
                per += "," + ((int)Math.Round(temp, 0));
                name += "|" + System.Net.WebUtility.UrlEncode(Helpers.FirstLetterToUpper(item.Key));
            }
            if (other > 1.0)
            {
                per += "," + ((int)Math.Round(other, 0));
                name += "|" + System.Net.WebUtility.UrlEncode(Helpers.FirstLetterToUpper(Dict.Other));
            }

            const string str = "https://chart.googleapis.com/chart?cht=p&chd=t:"; // "http://chart.apis.google.com/chart?cht=p&chd=t:";
            var url = str + per.Substring(1) + "&chs=450x250&chco=007acb&chl=" + name.Substring(1);
            return url;
        }
    }
}
