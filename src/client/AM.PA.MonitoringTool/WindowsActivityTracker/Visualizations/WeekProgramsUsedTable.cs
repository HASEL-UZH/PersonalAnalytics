// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2016-01-06
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
    class WeekProgramsUsedTable : BaseVisualization, IVisualization
    {
        private readonly DateTimeOffset _date;
        private const int MaxNumberOfPrograms = 7;

        public WeekProgramsUsedTable(DateTimeOffset date)
        {
            this._date = date;

            Title = "Top Programs Used during the Week"; //hint; overwritten below
            IsEnabled = true; //todo: handle by user
            Order = 3; //todo: handle by user
            Size = VisSize.Square;
            Type = VisType.Week;
        }

        public override string GetHtml()
        {
            var html = string.Empty;


            /////////////////////
            // fetch data sets
            /////////////////////
            var programUsePerDay = GetProgramUsePerDay();
            var totalHoursPerDay = GetTotalHoursPerDay(programUsePerDay);

            if (programUsePerDay.Count < 1)
            {
                html += VisHelper.NotEnoughData(Dict.NotEnoughData);
                return html;
            }


            /////////////////////
            // HTML
            /////////////////////

            html += string.Format(CultureInfo.InvariantCulture, "<table id='{0}'>", VisHelper.CreateChartHtmlTitle(Title));
            html += GetTableHeader();
            html += "<tbody style='overflow:hidden;'>";
            foreach (var prog in programUsePerDay)
            {
                html += "<tr>";
                html += "<td>" + prog.Key + "</td>";
                for (int i = 1; i < 7; i++)
                {
                    html += GetTableRow(GetPercentage(prog.Value.Days[i], totalHoursPerDay[i]));
                }
                html += GetTableRow(GetPercentage(prog.Value.Days[0], totalHoursPerDay[0])); // special case: to have Sunday last (TODO: make better, as it isn't the same in every culture)
                html += "</tr>";
            }
            html += "</tbody>";
            html += "</table>";


            /////////////////////
            // create & add javascript
            ////////////////////
            var js = "<script type='text/javascript'>"
                    + "var tf = new TableFilter('" + VisHelper.CreateChartHtmlTitle(Title) + "', { base_path: '/', "
                    + "col_widths:[ '9.6875em', '2.1875em', '2.1875em', '2.1875em', '2.1875em', '2.1875em', '2.1875em', '2.1875em'], " // fixed columns sizes
                    + "col_0: 'none', col_1: 'none', col_2: 'none', col_3: 'none', col_4: 'none', col_5: 'none', col_6: 'none', col_7: 'none', "
                    + "alternate_rows: true, " // styling options
                    + "grid_layout: true, grid_width: '25.6em', grid_height: '18em', grid_cont_css_class: 'grd-main-cont', grid_tblHead_cont_css_class: 'grd-head-cont', " // styling & behavior of the table                                                         
                    + "}); " // no content options
                    + "tf.init(); "
                    + "</script>";

            html += " " + js;

            return html;
        }

        private double GetPercentage(double programs, double total)
        {
            if (total == 0) return 0;
            return 1.0 / total * programs;
        }

        private string GetTableHeader()
        {
            var header = "<thead><tr><th>Program</th>";
            for (int i = 1; i < 7; i++)
            {
                header += "<th>" + DateTimeHelper.GetShortestDayName(i) + "</th>";
            }
            header += "<th>" + DateTimeHelper.GetShortestDayName(0) + "</th>"; // special case: to have Sunday last (TODO: make better, as it isn't the same in every culture)
            header += "</tr></thead>";
            return header;
        }

        private string GetTableRow(double perc)
        {
            var colorWithWeight = perc * 2;
            var percentage = ""; // Math.Round(GetPerc(prog.Value.Days[i], totalHoursPerDay[i]) * 100, 0) + "%";

            return "<td style='background-color:rgba(0,122,203, " + colorWithWeight + ");'>" + percentage + "</td>";
        }

        private Dictionary<string, Programs> GetProgramUsePerDay()
        {
            var dict = new Dictionary<string, Programs>();
            var first = DateTimeHelper.GetFirstDayOfWeek_Iso8801(_date);
            var last = DateTimeHelper.GetLastDayOfWeek_Iso8801(_date);

            // fetch & format data
            while (first <= last)
            {
                var programsDay = Queries.GetActivityPieChartData(first);
                var dayNumber = (int) first.DayOfWeek;

                foreach (var program in programsDay)
                {
                    var process = program.Key;
                    var dur = program.Value;

                    if (dict.ContainsKey(process))
                    {
                        dict[process].Days[dayNumber] += dur;
                    }
                    else
                    {
                        dict.Add(process, new Programs(dayNumber, dur));
                    }
                }
                
                first = first.AddDays(1);
            }

            // sort & filter
            var sortedDict = (from prog in dict orderby prog.Value.Total descending select prog).Take(MaxNumberOfPrograms).ToDictionary(pair => pair.Key, pair => pair.Value);

            return sortedDict;
        }

        private double[] GetTotalHoursPerDay(Dictionary<string, Programs> programs)
        {
            var total = new double[7];

            for (int i = 0; i < 7; i++)
            {
                foreach (var program in programs)
                {
                    total[i] += program.Value.Days[i];
                }
            }

            return total;
        }

    }

    internal class Programs
    {
        public double[] Days { get; set; }
        public double Total { get; set; }

        public Programs(int dayNumber, double dur)
        {
            Total = dur;
            Days = new double[7]; // one for every day
            Days[dayNumber] = dur;
        }
    }
}
