// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Shared;
using Shared.Helpers;
using WindowsActivityTracker.Data;
using System.Globalization;

namespace WindowsActivityTracker.Visualizations
{
    internal class DayProgramsUsedPieChart : BaseVisualization, IVisualization
    {
        private readonly DateTimeOffset _date;
        //private const int _maxNumberOfPrograms = 10;
        private const double _minTimeWorked = 0.3; // in hours

        public DayProgramsUsedPieChart(DateTimeOffset date)
        {
            this._date = date;

            Title = "Top Programs Used"; //hint; overwritten below
            IsEnabled = true; //todo: handle by user
            Order = 1; //todo: handle by user
            Size = VisSize.Square;
            Type = VisType.Day;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            /////////////////////
            // fetch data sets
            /////////////////////
            //var chartQueryPieChartData = new Dictionary<string, long>();
            var chartQueryResultsLocal = Queries.GetActivityPieChartData(_date);

            // merge with remote data if necessary //TODO: REMOTE DATA
            //chartQueryPieChartData = RemoteDataHandler.VisualizeWithRemoteData()
            //    ? RemoteDataHandler.MergeActivityData(chartQueryResultsLocal, Queries.GetActivityPieChartData(_date))
            //    : chartQueryResultsLocal;

            /////////////////////
            // data cleaning
            /////////////////////
            //// remove IDLE (doesn't belong to activity on computer)
            //if (chartQueryResultsLocal.ContainsKey(Dict.Idle))
            //    chartQueryResultsLocal.Remove(Dict.Idle);

            // calculate total active time
            var totalHoursWorked = chartQueryResultsLocal.Sum(x => x.Value);

            // check if we have enough data
            if (chartQueryResultsLocal.Count == 0 || totalHoursWorked < _minTimeWorked)
            {
                html += VisHelper.NotEnoughData(Dict.NotEnoughData);
                return html;
            }

            PrepareDataForVisualization(chartQueryResultsLocal);


            /////////////////////
            // HTML
            /////////////////////
            html += "<p style='text-align: center;'>Total hours worked on your computer: <strong>" + Math.Round(totalHoursWorked, 1) + "</strong>.</p>";
            html += "<div id='" + VisHelper.CreateChartHtmlTitle(Title) + "' style='height:75%;'  align='center'></div>";


            /////////////////////
            // JS
            /////////////////////
            var columns = string.Empty;
            foreach (var program in chartQueryResultsLocal)
            {
                columns += string.Format(CultureInfo.InvariantCulture, "['{0}', {1}], ", program.Key, Math.Round(program.Value, 1));
            }

            var data = "columns: [ " + columns + "], type: 'pie'";

            html += "<script type='text/javascript'>";
            html += "var " + VisHelper.CreateChartHtmlTitle(Title) + " = c3.generate({ bindto: '#" + VisHelper.CreateChartHtmlTitle(Title) + "', data: { " + data + "}, pie: { label: { format: function (value, ratio, id) { return value + 'h';}}}, padding: { top: 0, right: 0, bottom: 0, left: 0 }, legend: { show: true, position: 'bottom' }});";
            html += "</script>";

            return html;
        }

        /// <summary>
        /// Adds all items in the list (in case more than 10) to an Other group
        /// </summary>
        /// <param name="chartQueryResultsLocal"></param>
        private void PrepareDataForVisualization(Dictionary<string, double> chartQueryResultsLocal)
        {
            try
            {
                //if (chartQueryResultsLocal.Count >= _maxNumberOfPrograms)
                //{
                    // summarize small parts of work
                    var totalHoursWorked = chartQueryResultsLocal.Sum(x => x.Value);
                    var small = (totalHoursWorked > 1.0) ? totalHoursWorked * 0.05 : totalHoursWorked * 0.1; // put to 'other' either 5% or 10% of the processes
                    var keysToRemove = new List<string>();

                    foreach (var item in chartQueryResultsLocal.ToList())
                    {
                        if (item.Value < small)
                        {
                            if (chartQueryResultsLocal.ContainsKey(Dict.Other))
                            {
                                chartQueryResultsLocal[Dict.Other] += item.Value;
                            }
                            else
                            {
                                chartQueryResultsLocal.Add(Dict.Other, item.Value);
                            }
                            keysToRemove.Add(item.Key);
                        }
                    }

                    // remove OTHER if it is too small
                    if (chartQueryResultsLocal.ContainsKey(Dict.Other) && chartQueryResultsLocal[Dict.Other] <= small)
                        keysToRemove.Add(Dict.Other);

                    foreach (var key in keysToRemove)
                    {
                        chartQueryResultsLocal.Remove(key);
                    }
                //}

                Title = string.Format(CultureInfo.InvariantCulture, "Top {0} Programs Used", chartQueryResultsLocal.Count);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }
    }
}
