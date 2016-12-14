// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using BiometricsTracker.Data;
using Shared;
using Shared.Helpers;
using System;
using System.Collections.Generic;

namespace BiometricsTracker.Visualizations
{
    internal class BiometricVisualizationForDay : BaseVisualization, IVisualization
    {
        private DateTimeOffset date;

        public BiometricVisualizationForDay(DateTimeOffset date)
        {
            this.date = date;

            Title = "HR and HRV over the Day";
            IsEnabled = true;
            Order = 0;
            Size = VisSize.Wide;
            Type = VisType.Day;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            //Get Data
            List<Tuple<DateTime, double, double>> values = DatabaseConnector.GetBiometricValuesForDay(date);
            
            if (values.Count <= 1)
            {
                html += VisHelper.NotEnoughData("It is not possible to give you insights because there is not enough biometric data available.");
                return html;
            }
            
            // CSS
            html += "<style type='text/css'>";
            html += ".c3-line { stroke-width: 2px; }";
            html += ".c3-grid text, c3.grid line { fill: gray; }";
            html += ".axis path, .axis line {fill: none; stroke: grey; stroke-width: 1; shape-rendering: crispEdges;}";
            html += "</style>";

            //JS
            html += "<script>";
            html += "var margin = {top: 30, right: 40, bottom: 30, left: 50}, width = 1230 - margin.left - margin.right, height = 360 - margin.top - margin.bottom;";
            html += "var parseDate = d3.time.format('%Y-%m-%d %H:%M:%S').parse;";

            html += "var x = d3.time.scale().range([0, width]);";
            html += "var y0 = d3.scale.linear().range([height, 0]);";
            html += "var y1 = d3.scale.linear().range([height, 0]);";
            html += "var xAxis = d3.svg.axis().scale(x).orient('bottom').ticks(5).tickFormat(d3.time.format('%H:%M'));";
            html += "var yAxisLeft = d3.svg.axis().scale(y0).orient('left').ticks(5);";
            html += "var yAxisRight = d3.svg.axis().scale(y1).orient('right').ticks(5);";

            //html += "var valueLine1 = d3.svg.line().x(function(d) {return x(d.ts); }).y(function(d) { return y0(d.hr); });";
            html += "var valueLine1 = d3.svg.line().defined(function(d) {return d.hr != null; }).x(function(d) {return x(d.ts); }).y(function(d) { return y0(d.hr); });";
            html += "var valueLine2 = d3.svg.line().defined(function(d) {return d.hrv != null; }).x(function(d) {return x(d.ts); }).y(function(d) { return y1(d.hrv); });";

            html += "var svg = d3.select('body').append('svg').attr('width', width + margin.left + margin.righ).attr('height', height + margin.top + margin.bottom).append('g').attr('transform', 'translate(' + margin.left + ',' + margin.top + ')');";

            html += GetDataAsJSString(values);

            html += "x.domain(d3.extent(data, function(d) { return d.ts}));";
            html += "y0.domain([d3.min(data, function(d) {return Math.min(d.hr);}), d3.max(data, function(d) {return Math.max(d.hr);})]);";
            html += "y1.domain([d3.min(data, function(d) {return Math.min(d.hrv);}), d3.max(data, function(d) {return Math.max(d.hrv);})]);";

            html += "svg.append('path').style('stroke', 'blue').attr('d', valueLine1(data)).attr('fill', 'none');";
            html += "svg.append('path').style('stroke', 'red').attr('d', valueLine2(data)).attr('fill', 'none');";
            html += "svg.append('g').attr('class', 'x axis').attr('transform', 'translate(0,' + height + ')').call(xAxis);";
            html += "svg.append('g').attr('class', 'y axis').style('fill', 'steelblue').call(yAxisLeft);";
            html += "svg.append('g').attr('class', 'y axis').attr('transform', 'translate(' + width + ' ,0)').style('fill', 'red').call(yAxisRight);";
            
            html += "</script>";
            
            return html;
        }

        private static string GetDataAsJSString(List<Tuple<DateTime, double, double>> values)
        {
            var html = string.Empty;

            html += "var data = [";

            DateTime startTime = values[0].Item1;
            DateTime endTime = values[values.Count - 1].Item1;

            while (startTime != endTime)
            {
                List<Tuple<DateTime, double, double>> tuplesForThisSecond = values.FindAll(t => t.Item1.CompareTo(startTime) == 0);

                if (tuplesForThisSecond.Count == 0)
                {
                    html += "{'ts': parseDate('" + startTime.ToString("yyyy-MM-dd HH:mm:ss") + "'), 'hr': null" + ", 'hrv': null" + "},";
                }
                else
                {
                    for (int i = 0; i < tuplesForThisSecond.Count; i++)
                    {
                        if (tuplesForThisSecond[i].Item2 == 0)
                        {
                            if (i == 0)
                            {
                                html += "{'ts': parseDate('" + tuplesForThisSecond[i].Item1.ToString("yyyy-MM-dd HH:mm:ss") + "'), 'hr': " + tuplesForThisSecond[i+1].Item2 + ", 'hrv': " + tuplesForThisSecond[i].Item3 + "},";
                            }
                            else if (i + 1 == tuplesForThisSecond.Count)
                            {
                                html += "{'ts': parseDate('" + tuplesForThisSecond[i].Item1.ToString("yyyy-MM-dd HH:mm:ss") + "'), 'hr': " + tuplesForThisSecond[i-1].Item2 + ", 'hrv': " + tuplesForThisSecond[i].Item3 + "},";
                            }
                            else
                            {
                                html += "{'ts': parseDate('" + tuplesForThisSecond[i].Item1.ToString("yyyy-MM-dd HH:mm:ss") + "'), 'hr': " + ( (tuplesForThisSecond[i - 1].Item2 + tuplesForThisSecond[i - 1].Item2) / 2 ) + ", 'hrv': " + tuplesForThisSecond[i].Item3 + "},";
                            }
                        }
                        else
                        {
                            html += "{'ts': parseDate('" + tuplesForThisSecond[i].Item1.ToString("yyyy-MM-dd HH:mm:ss") + "'), 'hr': " + tuplesForThisSecond[i].Item2 + ", 'hrv': " + tuplesForThisSecond[i].Item3 + "},";
                        }
                    }

                }

                startTime = startTime.AddSeconds(1);
            }
            
            html = html.Remove(html.Length - 1);
            html += "];";

            return html;
        }
    }
}