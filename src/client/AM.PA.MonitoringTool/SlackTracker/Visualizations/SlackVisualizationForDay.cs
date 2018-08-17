// Created by Rohit Kaushik (rohit.kaushik@uzh.ch) from the University of Zurich
// Created: 2018-07-09
// 
// Licensed under the MIT License.

using Shared;
using System;
using SlackTracker.Data;
using Shared.Helpers;
using System.Collections.Generic;

namespace SlackTracker
{
    internal class SlackVisualizationForDay : BaseVisualization, IVisualization
    {
        private DateTimeOffset _date;

        public SlackVisualizationForDay(DateTimeOffset date)
        {
            Title = "Slack";
            this._date = date;
            IsEnabled = true;
            Size = VisSize.Square;
            Order = 0;
        }

        private string GenerateJSData(List<string> words)
        {
            string data = "[";
            int len = words.Count;

            for (int i = 0; i < len; i++)
            {
                data += "\"" + words[i] +"\"";

                if (i < len - 1) { data += ", ";}
            }

            data += "]";

            return data;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            List<string> value = DatabaseConnector.GetKeywordsForDate(_date.DateTime);

            if (value.Count == 0)
            {
                html += VisHelper.NotEnoughData();
                return html;
            }
            var key = GenerateJSData(value);

            //HTML
            html += "<svg id='visualization'></svg>";

            //SCRIPT
            html += "<script>";

            html += "var layout = d3.layout.cloud().size([500, 500]).words(" + key + ".map(function(d) {";
            html += "return {text: d, size: 10 + Math.random() * 90, test: \"haha\"};";
            html += "})).padding(5).rotate(function() {return ~~(Math.random() * 2) * 90; })";
            html += ".font(\"Impact\").fontSize(function(d) { return d.size; }).on(\"end\", draw);";

            html += "layout.start();";
            html += "function draw(words) {";
            html += "d3.select('#visualization')";
            html += ".attr(\"width\", layout.size()[0])";
            html += ".attr(\"height\", layout.size()[1])";
            html += ".append(\"g\")";
            html += ".attr(\"transform\", \"translate(\" + layout.size()[0] / 2 + \",\" + layout.size()[1] / 2 + \")\")";
            html += ".selectAll(\"text\").data(words).enter().append('text')";
            html += ".style(\"font-size\", function(d) { return d.size + \"px\"; })";
            html += ".style(\"font-family\", \"Impact\")";
            html += ".attr(\"text-anchor\", \"middle\")";
            html += ".attr(\"transform\", function(d) { return \"translate(\" + [d.x, d.y] + \")rotate(\" + d.rotate + \")\";})";
            html += ".text(function(d) { return d.text; });}";

            html += "</script>";

            return html;
        }
    }
}
