// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-04-13
// 
// Licensed under the MIT License.

using System;
using GoalSetting.Goals;
using Shared;
using Shared.Helpers;

namespace GoalSetting.Visualizers.Week
{
    public class WeekVisualizationForHourlyGoal : PAVisualization
    {
        public WeekVisualizationForHourlyGoal(DateTimeOffset date, GoalActivity goal) : base(date, goal) { }

        public override string GetHtml()
        {
            var html = string.Empty;
           
            // CSS
            html += "<style type='text/css'>";
            html += ".c3-line { stroke-width: 2px; }";
            html += ".c3-grid text, c3.grid line { fill: black; }";
            html += ".axis path, .axis line {fill: none; stroke: black; stroke-width: 1; shape-rendering: crispEdges;}";
            html += "</style>";

            //HTML
            html += "<div id='" + VisHelper.CreateChartHtmlTitle(Title) + "' style='align: center'></div>";
            html += "<p style='text-align: center; font-size: 0.66em;'>" + GoalVisHelper.GetHintText(_goal, VisType.Day) + "</p>";

            //JS
            html += "<script>";

            html += "var actualHeight = document.getElementsByClassName('item Wide')[0].offsetHeight;";
            html += "var actualWidth = document.getElementsByClassName('item Wide')[0].offsetWidth;";
            html += "var margin = {top: 10, right: 30, bottom: 30, left: 30}, width = (actualWidth * 0.97)- margin.left - margin.right, height = (actualHeight * 0.73) - margin.top - margin.bottom;";

            html += @"function gridData(totalWidth) {
	                    var data = new Array();
	                    var xpos = 1;
	                    var ypos = 1;
	                    var width = totalWidth / 25;
	                    var height = totalWidth / 25;
	                    var click = 0;
	                    var newValue = '';
                        var newType = 'Value';

                    	for (var row = 0; row < 8; row++) {
		                    data.push( new Array() );
                            for (var column = 0; column < 25; column++) {
			                    if (row == 0) {
                                    if (column != 0) {
                                        newValue = column - 1;
                                    }
                                } else if (column == 0) {
                                    if (row == 1) {
                                        newValue = 'Mo';
                                    }
                                    if (row == 2) {
                                        newValue = 'Di';
                                    }
                                    if (row == 3) {
                                        newValue = 'Mi';
                                    }
                                    if (row == 4) {
                                        newValue = 'Do';
                                    }
                                    if (row == 5) {
                                        newValue = 'Fr';
                                    }
                                    if (row == 6) {
                                        newValue = 'Sa';
                                    }
                                    if (row == 7) {
                                        newValue = 'So';
                                    }
                                } else {
                                    newValue = '';
                                }

                                if (row == 0 || column == 0) {
                                    newType = 'Title';
                                } else {
                                    newType = 'Value';
                                }

                                data[row].push({
                                type: newType,
				                x: xpos,
				                y: ypos,
				                width: width,
				                height: height,
                                value : newValue,
				                click: click
			                })
			                xpos += width;
		                }
            		xpos = 1;
		            ypos += height;	
	                }
	            return data;
                }";

            html += "var gridData = gridData(width);";
            html += "console.log(gridData);";

            html += "var grid = d3.select('#" + VisHelper.CreateChartHtmlTitle(Title) + "').append('svg')";
            html += @".attr('width', width + margin.left + margin.right).attr('height', height + margin.top + margin.bottom)
                    .append('g').attr('transform', 'translate(' + margin.left + ',' + margin.top + ')');";

            html += "var row = grid.selectAll('.row')";
            html += ".data(gridData)";
            html += ".enter().append('g')";
            html += ".attr('class', 'row');";

            html += "var column = row.selectAll('.square')";
            html += ".data(function(d) { return d; })";
            html += ".enter().append('rect')";
            html += ".attr('class', 'square')";
            html += ".attr('x', function(d) { return d.x; })";
            html += ".attr('y', function(d) { return d.y; })";
            html += ".attr('width', function(d) { return d.width; })";
            html += ".attr('height', function(d) { return d.height; })";
            html += ".style('fill', function(d) {if (d.type === 'Title') {return '#999';} return '#fff';})";
            html += ".style('stroke', '#222')";
            html += ".on('click', function(d) {";
            html += "d.click++;";
            html += "if (d.type === 'Value') {";
            html += "if ((d.click) % 4 == 0) { d3.select(this).style('fill', '#fff'); }";
            html += "if ((d.click) % 4 == 1) { d3.select(this).style('fill', '#2C93E8'); }";
            html += "if ((d.click) % 4 == 2) { d3.select(this).style('fill', '#F56C4E'); }";
            html += "if ((d.click) % 4 == 3) { d3.select(this).style('fill', '#838690'); }";
            html += "}});";

            html += "var text = row.selectAll('.label')";
            html += ".data(function(d) { return d; })";
            html += ".enter().append('svg:text')";
            html += ".attr('x', function(d) { return d.x + d.width / 2 })";
            html += ".attr('y', function(d) { return d.y + d.height / 2 })";
            html += ".attr('text-anchor', 'middle')";
            html += ".attr('dy', '.35em')";
            html += ".text(function(d) { return d.value });";

            html += "</script>";

            return html;
        }
    }
}