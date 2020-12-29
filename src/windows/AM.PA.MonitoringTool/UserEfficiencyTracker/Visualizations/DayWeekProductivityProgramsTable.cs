// Created by André Meyer at MSR
// Created: 2016-01-05
// 
// Licensed under the MIT License.

using System;
using System.Linq;
using Shared;
using UserEfficiencyTracker.Data;
using Shared.Helpers;
using System.Collections.Generic;
using Shared.Data;
using System.Globalization;

namespace UserEfficiencyTracker.Visualizations
{
    internal class DayWeekProductivityProgramsTable : BaseVisualization, IVisualization
    {
        private int maxNumberOfTopPrograms = 7;
        private readonly DateTimeOffset _date;
        private VisType _type;

        public DayWeekProductivityProgramsTable(DateTimeOffset date, VisType type)
        {
            this._date = date;
            this._type = type;

            Title = "Top Programs Used during (Un-)Productive Times";
            IsEnabled = true; //todo: handle by user
            Order = (type == VisType.Day) ? 7 : 1; //todo: handle by user
            Size = VisSize.Square;
            Type = type;
        }

        private string _notEnoughDataMsg = "It is not possible to give you insights into your productivity as you didn't fill out the pop-up often enough. Try to fill it out at least 3 times per day.";

        public override string GetHtml()
        {
            var html = string.Empty;

            /////////////////////
            // fetch data sets
            /////////////////////
            var prodResponses = Queries.GetUserProductivityTimelineData(_date, _type, true);
            var programsUsed = Queries.GetTopProgramsUsedWithTimes(_date, _type, maxNumberOfTopPrograms);

            if (prodResponses.Count < 3 || programsUsed.Count < 1)
            {
                html += VisHelper.NotEnoughData(_notEnoughDataMsg);
                return html;
            }

            /////////////////////
            // prepare data sets
            /////////////////////

            // create & initialize dictionary
            var dict = new Dictionary<string, MyPair>();

            foreach (var p in programsUsed)
            {
                if (dict.ContainsKey(p.Key)) continue;
                dict.Add(p.Key, new MyPair(0, 0));
            }

            // populate dictionary
            for (var i = 1; i < prodResponses.Count; i++)
            {
                // only count if PerceivedProductivity was either unproductive (1-3) or productive (5-7)
                var perceivedProductivity = prodResponses[i].Item2;
                if (perceivedProductivity > 7 || perceivedProductivity < 1 || perceivedProductivity == 4) continue;

                var intStart = prodResponses[i - 1].Item1;
                var intEnd = prodResponses[i].Item1;

                foreach (var program in programsUsed)
                {
                    foreach (var pItem in program.Value)
                    {
                        if (pItem.From > intStart && pItem.To < intEnd)
                        {
                            if (perceivedProductivity <= 3 && perceivedProductivity >= 1) dict[program.Key].Unproductive += pItem.DurInMins;
                            // prodResponses[i].Item2 = 4 is neutral
                            else if (perceivedProductivity >= 5 && perceivedProductivity <= 7) dict[program.Key].Productive += pItem.DurInMins;
                        }
                    }
                }
            }

            /////////////////////
            // visualize data sets
            /////////////////////
            var totalProductive = dict.Sum(i => i.Value.Productive);
            var totalUnproductive = dict.Sum(i => i.Value.Unproductive);

            // check if enough data is available
            if (totalProductive == 0 && totalUnproductive == 0)
            {
                html += VisHelper.NotEnoughData(_notEnoughDataMsg);
                return html;
            }

            // create blank table
            html += string.Format(CultureInfo.InvariantCulture, "<table id='{0}'>", VisHelper.CreateChartHtmlTitle(Title));
            html += "<thead><tr><th>Program</th><th>Productive</th><th>Unproductive</th></tr></thead>";
            html += "<tbody>";
            foreach (var p in dict)
            {
                var programUnproductive = (totalUnproductive == 0) ? 0 : Math.Round(100.0 / totalUnproductive * p.Value.Unproductive, 0);
                var programProductive = (totalProductive == 0) ? 0 : Math.Round(100.0 / totalProductive * p.Value.Productive, 0);

                html += "<tr>";
                html += "<td>" + ProcessNameHelper.GetFileDescription(p.Key) + "</td>";
                html += "<td style='color:green;'>" + programProductive + "%</td>";
                html += "<td style='color:red;'>" + programUnproductive + "%</td>";
                html += "</tr>";
            }
            html += "</tbody>";
            html += "</table>";

            /////////////////////
            // create & add javascript
            ////////////////////
            var js = "<script type='text/javascript'>"
                    + "var tf = new TableFilter('" + VisHelper.CreateChartHtmlTitle(Title) + "', { base_path: '/', "
                    + "col_widths:[ '12.4em', '6.25em', '6.25em'], " // fixed columns sizes
                    + "col_0: 'none', col_1: 'none', col_2: 'none', "
                    + "alternate_rows: true, " // styling options
                    + "grid_layout: true, grid_width: '25.6em', grid_height: '18em', grid_cont_css_class: 'grd-main-cont', grid_tblHead_cont_css_class: 'grd-head-cont', " // styling & behavior of the table       
                                                                                                                                                      //+ "extensions: [{name: 'sort', types: [ 'string', 'number', 'number'] }], "
                    + "}); " // no content options
                    + "tf.init(); "
                    + "</script>";

            html += " " + js;

            return html;
        }
    }

    internal class MyPair
    {
        public MyPair(int prod, int unprod)
        {
            Productive = prod;
            Unproductive = unprod;
        }

        public int Productive { get; set; }
        public int Unproductive { get; set; }
    }
}
