// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-13
// 
// Licensed under the MIT License.
//
// Adapted from bl.ocks.org/tjdecke/5558084
//

using System;
using Shared;
using PolarTracker.Data;
using Shared.Helpers;
using System.Collections.Generic;
using System.Globalization;

namespace PolarTracker
{
    internal class PolarVisualizationForWeek : BaseVisualization, IVisualization
    {
        private DateTimeOffset _date;

        public PolarVisualizationForWeek(DateTimeOffset date)
        {
            this._date = date;

            Title = "Heart rate and interbeat interval";
            IsEnabled = true;
            Order = -1;
            Size = VisSize.Wide;
            Type = VisType.Week;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            //Get Data
            var hrValues = DatabaseConnector.GetHRValuesForWeek(_date);
            var rmssdValues = DatabaseConnector.GetRMSSDValuesForWeek(_date);
            
            if (hrValues.Count < Settings.NUMBER_OF_BUCKETS || rmssdValues.Count < Settings.NUMBER_OF_BUCKETS)
            {
                html += VisHelper.NotEnoughData("It is not possible to give you insights because there is not enough biometric data available.");
                return html;
            }

            string[] dayNames = CultureInfo.InstalledUICulture.DateTimeFormat.AbbreviatedDayNames;

            //CSS
            html += "<style type='text/css'>";
            html += ".c3-line { stroke-width: 2px; }";
            html += ".c3-grid text, c3.grid line { fill: black; }";
            html += "rect.bordered { stroke: #E6E6E6; stroke-width:2px; }";
            html += "text.mono { font-size: 7.5pt; fill: #888; }";
            html += "text.axis-workweek { fill: #000; }";
            html += "text.axis-worktime { fill: #000; }";
            html += ".tooltip {background-color: white; box-shadow: 4px 4px 4px #888888; -webkit-box-shadow:2px 3px 4px #888888;padding:2px;position:absolute;top:0px;left:0px;visibility:hidden;border: solid 1px black;border-radius:5px;}";
            html += ".dataset-button { padding:0.3125em; background-color:white; border:1px solid " + Shared.Settings.RetrospectionColorHex + "; color:" + Shared.Settings.RetrospectionColorHex + "; text-decoration:none; margin:5px auto; }";
            html += ".dataset-button-active { padding:0.3125em; background-color:" + Shared.Settings.RetrospectionColorHex + "; border:1px solid " + Shared.Settings.RetrospectionColorHex + "; color: white; text-decoration:none; margin:5px auto; }";
            html += ".dataset-button:hover { background-color:" + Shared.Settings.RetrospectionColorHex + "; border:1px solid " + Shared.Settings.RetrospectionColorHex + "; color:white; cursor: pointer; cursor: hand; }";
            html += "</style>";

            //HTML
            html += "<div id='chart'></div>";
            html += "<div id='dataset-picker' style='float: right;'></div>";
            html += "<p style='text-align: center; font-size: 0.66em;'>Hint: Visualizes your heart rate (HR) and your interbeat interval (RMSSD). (Last time synced with BLE device: " + DatabaseConnector.GetLastTimeSynced().ToString(CultureInfo.InstalledUICulture) + ")</p>";
            html += "<div id='dataDiv' class='tooltip'></div>";

            //JS
            html += "<script type='text/javascript'>";
            html += "var actualHeight = document.getElementsByClassName('item Wide')[0].offsetHeight;";
            html += "var actualWidth = document.getElementsByClassName('item Wide')[0].offsetWidth;";
            html += "var margin = { top: 20, right: 0, bottom: 0, left: 40 },";
            html += "width = (actualWidth * 0.95) - margin.left - margin.right,";
            html += "height = (actualHeight * 0.73) - margin.top - margin.bottom,";

            html += "gridSize = Math.floor(width / 24), legendElementWidth = gridSize * 2,";
            html += "buckets = " + Settings.NUMBER_OF_BUCKETS + ",";
            html += GetColorRangeAsJSString(Settings.NUMBER_OF_BUCKETS, Shared.Settings.RetrospectionColorHex);
            html += "days = ['" + dayNames[(int)DayOfWeek.Monday] + "', '" + dayNames[(int)DayOfWeek.Tuesday] + "', '" + dayNames[(int)DayOfWeek.Wednesday] +"', '" + dayNames[(int)DayOfWeek.Thursday] + "', '" + dayNames[(int)DayOfWeek.Friday] + "', '" + dayNames[(int)DayOfWeek.Saturday] + "', '" + dayNames[(int)DayOfWeek.Sunday] + "'],";
            html += "times = ['00:00', '01:00', '02:00', '03:00', '04:00', '05:00', '06:00', '07:00', '08:00', '09:00', '10:00', '11:00', '12:00', '13:00', '14:00', '15:00', '16:00', '17:00', '18:00', '19:00', '20:00', '21:00', '22:00', '23:00'];";
       
            html += "var svg = d3.select('#chart').append('svg')";
            html += ".attr('width', width + margin.left + margin.right)";
            html += ".attr('height', height + margin.top + margin.bottom)";
            html += ".append('g')";
            html += ".attr('transform', 'translate(' + margin.left + ',' + margin.top + ')');";
                
            html += "var dayLabels = svg.selectAll('.dayLabel')";
            html += ".data(days)";
            html += ".enter().append('text')";
            html += ".text(function(d) { return d; })";
            html += ".attr('x', 0)";
            html += ".attr('y', function(d, i) { return i * gridSize; })";
            html += ".style('text-anchor', 'end')";
            html += ".attr('transform', 'translate(-6,' + gridSize / 1.5 + ')')";
            html += ".attr('class', function(d, i) { return ((i >= 0 && i <= 4) ? 'dayLabel mono axis axis-workweek' : 'dayLabel mono axis'); });";

            html += "var timeLabels = svg.selectAll('.timeLabel')";
            html += ".data(times)";
            html += ".enter().append('text')";
            html += ".text(function(d) { return d; })";
            html += ".attr('x', function(d, i) { return i * gridSize; })";
            html += ".attr('y', 0)";
            html += ".style('text-anchor', 'middle')";
            html += ".attr('transform', 'translate(' + gridSize / 2 + ', -6)')";
            html += ".attr('class', function(d, i) { return ((i >= 7 && i <= 16) ? 'timeLabel mono axis axis-worktime' : 'timeLabel mono axis'); });";

            html += GetDataAsJSString(hrValues, "hrdata");
            html += GetDataAsJSString(rmssdValues, "rmssddata");
            
            html += "var heatmapChart = function(data) {";
            
            html += "var colorScale = d3.scale.quantize().domain([d3.min(data, function(d) { return d.value; }), buckets - 1, d3.max(data, function(d) { return d.value; })]).range(colors);";

            html += "svg.selectAll('.hour').remove();";
            html += "var cards = svg.selectAll('.hour').data(data, function(d) { return d.day + ':' + d.hour; });";
            html += "cards.enter().append('rect').attr('x', function(d) { return (d.hour - 1) * gridSize; }).attr('y', function(d) { return (d.day - 1) * gridSize; }).attr('rx', 4).attr('ry', 4).attr('class', 'hour bordered').attr('width', gridSize).attr('height', gridSize).style('fill', colors[0]).attr('data', function(d) { return d.value}).attr('onmouseover', 'showData(evt)').attr('onmouseout', 'hideData(evt)');";
            html += "cards.transition().duration(1000).style('fill', function(d) { return colorScale(d.value); });";
            html += "cards.exit().remove();";

            html += "svg.selectAll('.legend').remove();";
            html += "var legend = svg.selectAll('.legend').data(colorScale.range(), function(d) { return d; });";
            html += "legend.enter().append('g').attr('class', 'legend');";
            html += "legend.append('rect').attr('x', function(d, i) { return legendElementWidth * i; }).attr('y', height-15).attr('width', legendElementWidth).attr('height', gridSize / 2).style('fill', function(d, i) { return colors[i]; });";
            html += "legend.append('text').attr('class', 'mono').text(function(d) { var r = colorScale.invertExtent(d); return '≥ ' + d3.format('.2f')(r[0]); }).attr('x', function(d, i) { return legendElementWidth * i; }).attr('y', height - (0.5 * gridSize));";
            html += "legend.exit().remove();";
            html += "};";
                
            html += "heatmapChart(hrdata);";

            html += "d3.select('#dataset-picker').append('input').attr('type', 'button').attr('value', 'HR').attr('class', 'dataset-button-active').attr('id', 'hrbutton').on('click', function() {document.getElementById('rmssdbutton');document.getElementById('rmssdbutton').style.backgroundColor='White';document.getElementById('rmssdbutton').style.color='black';this.style.backgroundColor = '" + Shared.Settings.RetrospectionColorHex + "';this.style.color='white';heatmapChart(hrdata);});";
            html += "d3.select('#dataset-picker').append('input').attr('type', 'button').attr('value', 'RMSSD').attr('class', 'dataset-button').attr('id', 'rmssdbutton').on('click', function() {document.getElementById('hrbutton');document.getElementById('hrbutton').style.backgroundColor='White';document.getElementById('hrbutton').style.color='black';this.style.backgroundColor = '" + Shared.Settings.RetrospectionColorHex + "';this.style.color='white';heatmapChart(rmssddata);});";

            html += "function showData(evt) { var target = evt.target;  target.setAttribute('opacity', '.8');";
            html += "var x = evt.clientX;";
            html += "var y = evt.clientY;";
            html += "var offsetX = window.pageXOffset;";
            html += "var offsetY = window.pageYOffset;";
            html += "dataDiv.style.left = 10 + x + offsetX + 'px';";
            html += "dataDiv.style.top = 20 + y + offsetY + 'px';";
            html += "var data = target.getAttribute('data');";
            html += "var html = data;";
            html += "dataDiv.innerHTML = html;";
            html += "dataDiv.style.visibility = 'visible';";
            html += "}";

            html += "function hideData(evt) {";
            html += "dataDiv.style.visibility = 'hidden';";
            html += "var target = evt.target;";
            html += "target.removeAttribute('opacity');";
            html += "}";

            html += "</script>";
            return html;
        }

        private static string GetColorRangeAsJSString(int numberOfBuckets, string colorHex)
        {
            var html = string.Empty;

            System.Windows.Media.Color color = (System.Windows.Media.Color) System.Windows.Media.ColorConverter.ConvertFromString(colorHex);

            html += "colors = [";

            for (double i = 1; i <= numberOfBuckets; i++)
            {
               html += "'rgba(" + color.R + ", " + color.G + ", " + color.B + ", " + 1/i + ")'" + ",";
            }

            html = html.Remove(html.Length - 1);
            html += "],";
            
            return html;
        }
        
        private static string GetDataAsJSString(List<Tuple<DateTime, double>> values, string datasetName)
        {
            var html = string.Empty;

            html += "var " + datasetName + " = [";

            bool hasAtLeastOneValidValue = false;
            foreach (Tuple<DateTime, double> t in values)
            {
                if (!Double.IsNaN(t.Item2))
                {
                    html += "{ day: " + GetNumericDayOfWeek(t.Item1.DayOfWeek) + ", hour: " + (t.Item1.Hour + 1) + ", value: " + Math.Round(t.Item2, 2, MidpointRounding.ToEven) + "},";
                    hasAtLeastOneValidValue = true;
                }
            }

            if (hasAtLeastOneValidValue)
            {
                html = html.Remove(html.Length - 1);
            }
            html += "];";
        
            return html;
        }

        private static int GetNumericDayOfWeek(DayOfWeek day)
        {
            switch(day)
            {
                case DayOfWeek.Monday: return 1;
                case DayOfWeek.Tuesday: return 2;
                case DayOfWeek.Wednesday: return 3;
                case DayOfWeek.Thursday: return 4;
                case DayOfWeek.Friday: return 5;
                case DayOfWeek.Saturday: return 6;
                case DayOfWeek.Sunday: return 7;
                default: return int.MinValue;
            }
        }
    }
}