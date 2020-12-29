// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using PolarTracker.Data;
using Shared;
using Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace PolarTracker.Visualizations
{
    internal class PolarVisualizationForDay : BaseVisualization, IVisualization
    {
        private const string TIME_FORMAT = "yyyy-MM-dd HH:mm";
        private DateTimeOffset _date;

        public PolarVisualizationForDay(DateTimeOffset date)
        {
            this._date = date;

            Title = "Heart rate and interbeat interval";
            IsEnabled = true;
            Order = 0;
            Size = VisSize.Wide;
            Type = VisType.Day;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            //Get Data
            List<Tuple<DateTime, double, double>> values = DatabaseConnector.GetPolarValuesForDay(_date);
            
            if (values.Count <= 3)
            {
                html += VisHelper.NotEnoughData("It is not possible to give you insights because there is not enough biometric data available.");
                return html;
            }
            
            // CSS
            html += "<style type='text/css'>";
            html += ".c3-line { stroke-width: 2px; }";
            html += ".c3-grid text, c3.grid line { fill: black; }";
            html += ".axis path, .axis line {fill: none; stroke: black; stroke-width: 1; shape-rendering: crispEdges;}";
            html += "</style>";

            //HTML
            html += "<div id='chart' style='align: center'></div>";
            html += "<p style='text-align: center; font-size: 0.66em;'>Hint: Visualizes your heart rate (HR) and your interbeat interval (RMSSD). (Last time synced with BLE device: " + DatabaseConnector.GetLastTimeSynced().ToString(CultureInfo.InstalledUICulture) + ")</p>";

            //JS
            html += "<script>";
            html += "var actualHeight = document.getElementsByClassName('item Wide')[0].offsetHeight;";
            html += "var actualWidth = document.getElementsByClassName('item Wide')[0].offsetWidth;";
            html += "var margin = {top: 30, right: 30, bottom: 30, left: 30}, width = (actualWidth * 0.97)- margin.left - margin.right, height = (actualHeight * 0.73) - margin.top - margin.bottom;";
            html += "var parseDate = d3.time.format('%Y-%m-%d %H:%M').parse;";

            html += GetDataAsJSString(values);
            
            html += "var x = d3.time.scale().range([0, width]);";
            html += "var y0 = d3.scale.linear().range([height, 0]);";
            html += "var y1 = d3.scale.linear().range([height, 0]);";

            if (GetDurationInHours(values) >= 12)
            {
                html += "var xAxis = d3.svg.axis().scale(x).orient('bottom').ticks(d3.time.hours, 2).tickFormat(d3.time.format('%H:%M'));";
            }
            else if (GetDurationInHours(values) > 3)
            {
                html += "var xAxis = d3.svg.axis().scale(x).orient('bottom').ticks(d3.time.hours, 1).tickFormat(d3.time.format('%H:%M'));";
            }
            else
            {
                html += "var xAxis = d3.svg.axis().scale(x).orient('bottom').ticks(d3.time.minutes, 15).tickFormat(d3.time.format('%H:%M'));";
            }
            
            html += "var yAxisLeft = d3.svg.axis().scale(y0).orient('left').ticks(5);";
            html += "var yAxisRight = d3.svg.axis().scale(y1).orient('right').ticks(5);";

            html += "var valueLine1 = d3.svg.line().interpolate('basis').defined(function(d) {return d.hr != null; }).x(function(d) {return x(d.ts); }).y(function(d) { return y0(d.hr); });";
            html += "var valueLine2 = d3.svg.line().interpolate('basis').defined(function(d) {return d.rmssd != null; }).x(function(d) {return x(d.ts); }).y(function(d) { return y1(d.rmssd); });";

            html += "var svg = d3.select('#chart').append('svg').attr('width', width + margin.left + margin.righ).attr('height', height + margin.top + margin.bottom).append('g').attr('transform', 'translate(' + margin.left + ',' + margin.top + ')');";
            
            html += "x.domain(d3.extent(data, function(d) { return d.ts}));";
            html += "var hrValues = data.map(function(o){return o.hr;}).filter(function(val) {return val !== null});";
            html += "var rmssdValues = data.map(function(o){return o.rmssd;}).filter(function(val) {return val !== null});";

            html += "y0.domain([d3.min(hrValues) * 0.95, d3.max(data, function(d) {return Math.max(d.hr);}) * 1.01]);";
            html += "y1.domain([d3.min(rmssdValues) * 0.95, d3.max(data, function(d) {return Math.max(d.rmssd);}) * 1.01]);";

            html += "svg.append('path').style('stroke', '" + Shared.Settings.RetrospectionColorHex + "').attr('d', valueLine1(data)).attr('fill', 'none');";
            html += "svg.append('path').style('stroke', '#ff7f0e').attr('d', valueLine2(data)).attr('fill', 'none');";

            if (HasAtLeastOneValidHRValue(values))
            {
                html += "svg.append('svg:line').style('stroke', '" + Shared.Settings.RetrospectionColorHex + "').attr('x1', 0).attr('x2', width).attr('y1', y0(" + GetAverageHeartrate(values) + ")).attr('y2', y0(" + GetAverageHeartrate(values) + ")).style('stroke-dasharray', ('12, 9')).style('opacity', 0.4);";
            }
            
            if (HasAtLeastOneValidRMSSDValue(values))
            {
                html += "svg.append('svg:line').style('stroke', '#ff7f0e').attr('x1', 0).attr('x2', width).attr('y1', y1(" + GetAverageRMSSD(values) + ")).attr('y2', y1(" + GetAverageRMSSD(values) + ")).style('stroke-dasharray', ('12, 9')).style('opacity', 0.4);";
            }

            html += "svg.append('g').attr('class', 'x axis').attr('transform', 'translate(0,' + height + ')').call(xAxis);";
            html += "svg.append('g').attr('class', 'y axis').style('fill', '" + Shared.Settings.RetrospectionColorHex + "').call(yAxisLeft);";
            html += "svg.append('g').attr('class', 'y axis').attr('transform', 'translate(' + width + ' ,0)').style('fill', '#ff7f0e').call(yAxisRight);";

            html += "svg.append('text').attr('x', 0).attr('y', -10).style('text-anchor', 'middle').text('HR');";
            html += "svg.append('text').attr('x', width).attr('y', -10).style('text-anchor', 'middle').text('RMSSD');";

            html += "</script>";
            
            return html;
        }

        private int GetDurationInHours(List<Tuple<DateTime, double, double>> values)
        {
            if (values.Count < 2)
            {
                return 0;
            }
            else
            {
               return values[values.Count - 1].Item1.Hour - values[0].Item1.Hour;
            }
        }

        private bool HasAtLeastOneValidHRValue(List<Tuple<DateTime, double, double>> values)
        {
            foreach (Tuple<DateTime, double, double> t in values)
            {
                if (!Double.IsNaN(t.Item2))
                {
                    return true;
                }
            }
            return false;
        }

        private bool HasAtLeastOneValidRMSSDValue(List<Tuple<DateTime, double, double>> values)
        {
            foreach (Tuple<DateTime, double, double> t in values)
            {
                if (!Double.IsNaN(t.Item3))
                {
                    return true;
                }
            }
            return false;
        }

        private double GetAverageHeartrate(List<Tuple<DateTime, double, double>> values)
        {
            double sum = 0;
            double count = 0;
            foreach (Tuple<DateTime, double, double> t in values)
            {
                if (!Double.IsNaN(t.Item2))
                {
                    count++;
                    sum += t.Item2;
                }
            }
            double average = sum / count;
            return average;
        }

        private double GetAverageRMSSD(List<Tuple<DateTime, double, double>> values)
        {
            double sum = 0;
            double count = 0;
            foreach (Tuple<DateTime, double, double> t in values)
            {
                if (!Double.IsNaN(t.Item3) && t.Item3 <= Settings.RR_DIFFERENCE_THRESHOLD)
                {
                    count++;
                    sum += t.Item3;
                }
            }
            double average = sum / count;
            return average;
        }

        private static string GetDataAsJSString(List<Tuple<DateTime, double, double>> values)
        {
            var html = string.Empty;

            html += "var data = [";

            DateTime startTime = values[0].Item1;
            DateTime endTime = values[values.Count - 1].Item1;

            while (startTime != endTime)
            {
                List<Tuple<DateTime, double, double>> tuplesForThisMinute = values.FindAll(t => t.Item1.CompareTo(startTime) == 0);

                if (tuplesForThisMinute.Count == 0)
                {
                    html += "{'ts': parseDate('" + startTime.ToString(TIME_FORMAT) + "'), 'hr': null" + ", 'rmssd': null" + "},";
                }
                else
                {
                    for (int i = 0; i < tuplesForThisMinute.Count; i++)
                    {
                        if (Double.IsNaN(tuplesForThisMinute[i].Item2) && Double.IsNaN(tuplesForThisMinute[i].Item3))
                        {
                            //do nothing, discard this datapoint
                        }
                        else if (tuplesForThisMinute[i].Item2 == 0 || Double.IsNaN(tuplesForThisMinute[i].Item2))
                        {
                            if (i == 0)
                            {
                                if (tuplesForThisMinute.Count == 1) { continue; }
                                html += "{'ts': parseDate('" + tuplesForThisMinute[i].Item1.ToString(TIME_FORMAT) + "'), 'hr': " + tuplesForThisMinute[i + 1].Item2 + ", 'rmssd': " + ( (Double.IsNaN(tuplesForThisMinute[i].Item3) || (tuplesForThisMinute[i].Item3 > Settings.RR_DIFFERENCE_THRESHOLD)) ? "null" : tuplesForThisMinute[i].Item3.ToString() ) + "},";
                            }
                            else if (i + 1 == tuplesForThisMinute.Count)
                            {
                                html += "{'ts': parseDate('" + tuplesForThisMinute[i].Item1.ToString(TIME_FORMAT) + "'), 'hr': " + tuplesForThisMinute[i - 1].Item2 + ", 'rmssd': " + ((Double.IsNaN(tuplesForThisMinute[i].Item3) || (tuplesForThisMinute[i].Item3 > Settings.RR_DIFFERENCE_THRESHOLD)) ? "null" : tuplesForThisMinute[i].Item3.ToString()) + "},";
                            }
                            else
                            {
                                html += "{'ts': parseDate('" + tuplesForThisMinute[i].Item1.ToString(TIME_FORMAT) + "'), 'hr': " + ((tuplesForThisMinute[i - 1].Item2 + tuplesForThisMinute[i - 1].Item2) / 2) + ", 'rmssd': " + ((Double.IsNaN(tuplesForThisMinute[i].Item3) || (tuplesForThisMinute[i].Item3 > Settings.RR_DIFFERENCE_THRESHOLD)) ? "null" : tuplesForThisMinute[i].Item3.ToString()) + "},";
                            }
                        }
                        else
                        {
                            html += "{'ts': parseDate('" + tuplesForThisMinute[i].Item1.ToString(TIME_FORMAT) + "'), 'hr': " + tuplesForThisMinute[i].Item2 + ", 'rmssd': " + ((Double.IsNaN(tuplesForThisMinute[i].Item3) || (tuplesForThisMinute[i].Item3 > Settings.RR_DIFFERENCE_THRESHOLD)) ? "null" : tuplesForThisMinute[i].Item3.ToString()) + "},";
                        }
                    }
                }
                startTime = startTime.AddMinutes(1);
            }
            
            html = html.Remove(html.Length - 1);
            html += "];";

            return html;
        }
    }
}