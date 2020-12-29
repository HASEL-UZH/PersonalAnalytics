// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-04
// 
// Licensed under the MIT License.

using System;
using Shared;
using FitbitTracker.Data;
using Shared.Helpers;
using System.Collections.Generic;

namespace FitbitTracker
{
    internal class SleepVisualizationForWeek : BaseVisualization, IVisualization
    {
        private DateTimeOffset _date;

        public SleepVisualizationForWeek(DateTimeOffset date)
        {
            this._date = date;
            Title = "Sleep per Week Day";
            Size = VisSize.Wide;
            IsEnabled = true;
            Order = -10;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            List<SleepVisualizationForWeekEntry> values = DatabaseConnector.GetSleepDataForWeek(DateTimeHelper.GetFirstDayOfWeek_Iso8801(_date), DateTimeHelper.GetLastDayOfWeek_Iso8801(_date));

            if (values == null || values.Count == 0)
            {
                html += VisHelper.NotEnoughData();
                return html;
            }

            //HTML
            html += "<div id='sleepVis'></div>";
            html += "<div id='legend'></div>";
            html += "<p style='text-align: center; font-size: 0.66em;'>Hint: Visualizes your sleep stats for this week. For more detailed information, visit: <a href='http://fitbit.com' target=_blank>fitbit.com</a>. (Last synced: " + DatabaseConnector.GetLastTimeSynced() + ").</p>";

            //JS
            html += "<script>";

            html += "var actualHeight = document.getElementsByClassName('item Wide')[0].offsetHeight;";
            html += "var actualWidth = document.getElementsByClassName('item Wide')[0].offsetWidth;";

            html += "var color1 = d3.scale.category10();";

            html += "var svg = d3.select('#legend').append('svg').attr('width', actualWidth * 1.1).attr('height', 30);";
            html += "svg.selectAll('legRec')";
            html += ".data([{C:'Asleep', XA:80}, {C:'Awake', XA:230}, {C:'Restless', XA:380}])";
            html += ".enter().append('text')";
            html += ".attr('class', 'bartext')";
            html += ".attr('text-anchor', 'middle')";
            html += ".attr('fill', 'black')";
            html += ".attr('x', function(d) {";
            html += "return d.XA;";
            html += "})";
            html += ".attr('y', 18)";
            html += ".text(function(d){";
            html += "return d.C;";
            html += "});";

            html += "svg.selectAll('legRec')";
            html += ".data([{C:'Asleep', XA:30}, {C:'Awake', XA:180}, {C:'Restless', XA:330}])";
            html += ".enter().append('rect')";
            html += ".attr('fill', function(d) {";
            html += " return color1(d.C);";
            html += "})";
            html += ".attr('x', function(d) {";
            html += "return d.XA;";
            html += "})";
            html += ".attr('y', function(d) {";
            html += "return 5;";
            html += "})";
            html += ".attr('height', function(d) {";
            html += "return 15;";
            html += "})";
            html += ".attr('width', 15);";

            html += GenerateData(values, DateTimeHelper.GetFirstDayOfWeek_Iso8801(_date), DateTimeHelper.GetLastDayOfWeek_Iso8801(_date));
            html += "var xData = ['A', 'B', 'C'];";
            
            html += "var margin = { top: 30, right: 50, bottom: 5, left: 50},";
            html += "width = actualWidth * 1.1 - margin.left - margin.right,";
            html += "height = (actualHeight * 0.65) - margin.top - margin.bottom;";

            html += "var x = d3.scale.ordinal().rangeRoundBands([0, width], .35);";
            html += "var y = d3.scale.linear().rangeRound([height, 0]);";

            html += "var color = d3.scale.category10();";
            html += "var xAxis = d3.svg.axis().scale(x).orient('bottom');";

            html += "var svg = d3.select('#sleepVis').append('svg')";
            html += ".attr('width', width + margin.left + margin.right)";
            html += ".attr('height', height + margin.top + margin.bottom)";
            html += ".append('g')";
            html += ".attr('transform', 'translate(margin.left, margin.top)');";

            html += "var dataIntermediate = xData.map(function(c) {";
            html += "return data.map(function(d) {";
            html += "return { x: d.day, y: d[c]};";
            html += "});});";

            html += "var dataStackLayout = d3.layout.stack()(dataIntermediate);";
            html += "x.domain(dataStackLayout[0].map(function(d) {";
            html += "return d.x;";
            html += "}));";

            html += "y.domain([0,";
            html += "d3.max(dataStackLayout[dataStackLayout.length - 1],";
            html += "function(d) { return d.y0 + d.y; })";
            html += "])";
            html += ".nice();";

            html += "var layer = svg.selectAll('.stack')";
            html += ".data(dataStackLayout)";
            html += ".enter().append('g')";
            html += ".attr('class', 'stack')";
            html += ".style('fill', function(d, i) {";
            html += "return color(i);";
            html += "});";
            
            html += "layer.selectAll('rect')";
            html += ".data(function(d) {";
            html += "return d;";
            html += "})";
            html += ".enter().append('rect')";
            html += ".attr('x', function(d) {";
            html += "return x(d.x);";
            html += "})";
            html += ".attr('y', function(d) {";
            html += "return y(d.y + d.y0);";
            html += "})";
            html += ".attr('height', function(d) {";
            html += "return y(d.y0) - y(d.y + d.y0);";
            html += "})";
            html += ".attr('width', x.rangeBand());";

            html += "var yTextPadding = 30;";
            html += "svg.selectAll('.bartext')";
            html += ".data(textData)";
            html += ".enter().append('text')";
            html += ".attr('class', 'bartext')";
            html += ".attr('text-anchor', 'middle')";
            html += ".attr('fill', 'white')";
            html += ".attr('x', function(d) {";
            html += "return x(d.day) + x.rangeBand() / 2;";
            html += "})";
            html += ".attr('y', function(d) {";
            html += "return height - yTextPadding;";
            html += "})";
            html += ".html(function(d){";
            html += "return (d.A / 60).toFixed(2) + 'h';";
            html += "});";

            html += "var yTextPadding = 10;";
            html += "svg.selectAll('.totaltext')";
            html += ".data(totalData)";
            html += ".enter().append('text')";
            html += ".attr('class', 'totaltext')";
            html += ".attr('text-anchor', 'middle')";
            html += ".attr('fill', 'white')";
            html += ".attr('x', function(d) {";
            html += "return x(d.day) + x.rangeBand() / 2;";
            html += "})";
            html += ".attr('y', function(d) {";
            html += "return height - yTextPadding;";
            html += "})";
            html += ".html(function(d){";
            html += "return '(&sum; ' + (d.T / 60).toFixed(2) + 'h)';";
            html += "});";

            html += "svg.append('g')";
            html += ".attr('class', 'axis')";
            html += ".attr('transform', 'translate(0,' + height + ')')";
            html += ".call(xAxis);";

            html += "</script>";

            return html;
        }

        private string GenerateData(List<SleepVisualizationForWeekEntry> values, DateTimeOffset start, DateTimeOffset end)
        {
            string data = string.Empty;
            string textData = string.Empty;
            string totalData = string.Empty;

            data += "[\n";
            textData += "[\n";
            totalData += "[\n";

            for (DateTimeOffset date = start; date <= end; date = date.AddDays(1))
            {
                SleepVisualizationForWeekEntry entry = values.Find(x => x.Day.DayOfWeek == date.DayOfWeek);
                
                if (entry != null)
                {
                    totalData += "{day:'" + entry.Day.DayOfWeek.ToString() + "', T:" + (entry.SleepDuration + entry.RestlessDuration + entry.AwakeDuration) + "},\n";
                    textData += "{day:'" + entry.Day.DayOfWeek.ToString() + "', A:" + entry.SleepDuration + "},\n";
                    data += entry.ToDataString() + ",\n";
                }
                else
                {
                    data += "{day:'" + date.DayOfWeek.ToString() + "', A:0, B:0, C:0},\n";
                }
 
            }

            data = data.Substring(0, data.Length - 2);
            textData = textData.Substring(0, textData.Length - 2);
            totalData = totalData.Substring(0, totalData.Length - 2);

            data += "\n]";
            textData += "\n]";
            totalData += "\n]";

            data = "var data = " + data + ";";
            textData = "var textData = " + textData + ";";
            totalData = "var totalData = " + totalData + ";";

            return data + textData + totalData;
        }

    }

    public class SleepVisualizationForWeekEntry
    {
        public DateTime Day { get; set; }

        public int SleepDuration { get; set; }

        public int AwakeDuration { get; set; }

        public int RestlessDuration { get; set; }

        public string ToDataString()
        {
            return "{day:'" + Day.DayOfWeek.ToString() + "', A:" + SleepDuration + ", B:" + AwakeDuration + ", C:" + RestlessDuration + "}";
        }
    }

}