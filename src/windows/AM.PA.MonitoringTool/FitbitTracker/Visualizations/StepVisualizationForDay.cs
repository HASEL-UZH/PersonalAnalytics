// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-30
// 
// Licensed under the MIT License.

using System;
using Shared;
using FitbitTracker.Data;
using Shared.Helpers;
using System.Collections.Generic;

namespace FitbitTracker
{
    internal class StepVisualizationForDay : BaseVisualization, IVisualization
    {
        public readonly int MINUTES_PER_BAR = 60;

        private DateTimeOffset _date;

        public StepVisualizationForDay(DateTimeOffset date)
        {
            Title = "Steps per " + MINUTES_PER_BAR + " Minutes";
            this._date = date;
            IsEnabled = true;
            Size = VisSize.Wide;
            Order = 0;
        }

        private string GenerateJSData(List<Tuple<DateTime, int>> values)
        {
            string data = "var barData = [";
            
            int first = (values.FindIndex(x => x.Item2 != 0));
            int last = (values.FindLastIndex(x => x.Item2 != 0));
            
            double sum = 0;
            for (int i = 0; i < values.Count; i++)
            {
                sum += values[i].Item2;
            }

            double average = sum / ((last - first) + 1);

            for (int i = first; i <= last; i++)
            {
                data += "{'x':'" + values[i].Item1.ToString(Settings.FORMAT_TIME) + "', 'y':" + values[i].Item2 + ", 'a':" + average + "},";
            }

            data = data.Substring(0, data.Length - 1);
            data += "];";
            
            return data;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            List<Tuple<DateTime, int>> values = DatabaseConnector.GetStepsPerTimeFraction(DateTimeHelper.GetStartOfDay(_date), DateTimeHelper.GetEndOfDay(_date), MINUTES_PER_BAR);
            
            if (values.FindAll(x => x.Item2 != 0).Count < 1)
            {
                html += VisHelper.NotEnoughData();
                return html;
            }
            
            //CSS
            html += "<style>";

            html += ".axis path,.axis line {";
            html += "fill: none;";
            html += "stroke: #000;";
            html += "shape - rendering: crispEdges;";
            html += "}";

            html += ".axis text {";
            html += "}";

            html += ".tick {";
            html += "stroke - dasharray: 1, 2;";
            html += "}";

            html += ".bar {";
            html += "fill: FireBrick;";
            html += "}";

            html += "</style>";

            //HTML
            html += "<svg id='visualisation'></svg>";
            html += "<p style='text-align: center; font-size: 0.66em;'>Hint: Visualizes your steps per " + MINUTES_PER_BAR + " minutes. For more detailed information, visit: <a href='http://fitbit.com' target=_blank>fitbit.com</a>. (Last synced: " + DatabaseConnector.GetLastTimeSynced() + ").</p>";

            //SCRIPT
            html += "<script>";

            html += "InitChart();";

            html += "function InitChart() {";

            html += GenerateJSData(values);

            html += "var actualHeight = document.getElementsByClassName('item Wide')[0].offsetHeight;";
            html += "var actualWidth = document.getElementsByClassName('item Wide')[0].offsetWidth;";
            html += "var vis = d3.select('#visualisation'),";
            html += "WIDTH = actualWidth, HEIGHT = actualHeight * 0.75,";
            html += "MARGINS = {top: 20, right: 20, bottom: 20, left: 50";
            html += "},";
            
            html += "xRange = d3.scale.ordinal().rangeRoundBands([MARGINS.left, WIDTH - MARGINS.right], 0.1).domain(barData.map(function(d) {";
            html += "return d.x;";
            html += "})),";
            
            html += "yRange = d3.scale.linear().range([HEIGHT - MARGINS.top, MARGINS.bottom]).domain([0,";
            html += "d3.max(barData, function(d) {";
            html += "return d.y;";
            html += "})]),";
            
            html += "xAxis = d3.svg.axis()";
            html += ".scale(xRange)";
            html += ".tickValues([" + GenerateTicks(values) + "])";
            html += ".tickSize(5),";
           
            html += "yAxis = d3.svg.axis().scale(yRange).tickSize(5).orient('left').tickSubdivide(true);";

            html += "vis.attr('height', HEIGHT).attr('width', WIDTH);";
            html += "vis.append('svg:g').attr('class', 'x axis').attr('transform', 'translate(0,' + (HEIGHT - MARGINS.bottom) + ')').call(xAxis);";
            html += "vis.append('svg:g').attr('class', 'y axis').attr('transform', 'translate(' + (MARGINS.left) + ',0)').call(yAxis);";
            
            html += "vis.selectAll('rect').data(barData).enter().append('rect').attr('x', function(d) {";
            html += "return xRange(d.x);";
            html += "})";
            html += ".attr('y', function(d) {";
            html += "return yRange(d.y);";
            html += "})";
            html += ".attr('width', xRange.rangeBand())";
            html += ".attr('height', function(d) {";
            html += "return ((HEIGHT - MARGINS.bottom) - yRange(d.y));";
            html += "})";
            html += ".attr('fill', '" + Shared.Settings.RetrospectionColorHex + "')";
            html += ".on('mouseover', function(d) {";
            html += "d3.select(this).attr('fill', '" + Shared.Settings.GrayColorHex + "');";
            html += "document.getElementById(d.x).style.opacity='0';";
            html += "document.getElementById('average').style.opacity='1';";
            html += "document.getElementById('avg' + d.x).style.opacity='1';";
            html += "})";
            html += ".on('mouseout', function(d) {";
            html += "d3.select(this)";
            html += ".attr('fill', '" + Shared.Settings.RetrospectionColorHex + "');";
            html += "document.getElementById(d.x).style.opacity='1';";
            html += "document.getElementById('average').style.opacity='0';";
            html += "document.getElementById('avg' + d.x).style.opacity='0';";
            html += "});";

            html += "var valueLine1 = d3.svg.line().x(function(d) {return xRange(d.x); }).y(function(d) { return yRange(d.a); });";
            html += "vis.append('path').style('stroke', '" + Shared.Settings.RetrospectionColorHex + "').attr('d', valueLine1(barData)).attr('fill', 'none').attr('id', 'average').attr('opacity', 0);";

            html += "vis.selectAll('avgText').data(barData).enter().append('text')";
            html += ".attr('x', function(d){return xRange(d.x) + xRange.rangeBand() / 2;})";
            html += ".attr('y', function(d){return yRange(d.a) - 5;})";
            html += ".attr('fill', '" + Shared.Settings.RetrospectionColorHex + "')";
            html += ".attr('text-anchor', 'middle')";
            html += ".attr('id', function(d){return 'avg' + d.x;})";
            html += ".attr('opacity', 0)";
            html += ".text(function(d){return (d.y - d.a).toFixed(0);});";

            html += "vis.selectAll('legText').data(barData).enter().append('text').attr('x', function(d) {";
            html += "return xRange(d.x) + xRange.rangeBand() / 2;";
            html += "})";
            html += ".attr('id', function(d) {";
            html += "return d.x";
            html += "})";
            html += ".attr('text-anchor', 'middle')";
            html += ".attr('fill', 'black')";
            html += ".attr('y', function(d) {";
            html += "return yRange(d.y) - 5;";
            html += "})";
            html += ".text(function(d) {";
            html += "if (d.y > 0) {";
            html += "return d.y;}";
            html += "else{return '';}})";
            html += ";}";
            
            html += "</script>";
            
            return html;
        }

        private string GenerateTicks(List<Tuple<DateTime, int>> values)
        {
            string ticks = string.Empty;

            int first = (values.FindIndex(x => x.Item2 != 0));
            int last = (values.FindLastIndex(x => x.Item2 != 0));

            for (int i = first; i <= last; i = i + 2)
            {
                ticks += "'" + values[i].Item1.ToString(Settings.FORMAT_TIME) + "',";
            }

            if (!ticks.Equals(string.Empty))
            {
                ticks = ticks.Substring(0, ticks.Length - 1);
            }

            return ticks;
        }
    }

}