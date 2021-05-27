using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsActivityTracker.Data;
using Shared;
using Shared.Helpers;
using WindowsActivityTracker.Models;
using System.Diagnostics;
using System.IO;

namespace WindowsActivityTracker.Visualizations
    {
        internal class DayWindowTitleList : BaseVisualization, IVisualization
        {
            private readonly DateTimeOffset _date;
            private const int TimelineZoomFactor = 1; // shouldn't be 0!, if > 1 then the user can scroll on top of the timeline
            private StringBuilder _sb = new StringBuilder();

            public DayWindowTitleList(DateTimeOffset date)
            {
                this._date = date;
                Title = "Timeline: Window Title List";
                IsEnabled = true; //todo: handle by user
                Order = 2; //todo: handle by user
                Size = VisSize.Wide;
                Type = VisType.Day;
                Console.WriteLine("test print statement");
        }

  

            public override string GetHtml()
            {

            
            var html = string.Empty;

            /////////////////////
            // fetch data sets
            /////////////////////
            var orderedTimelineList = Queries.GetDayWindowTitleData(_date);

            /////////////////////
            // data cleaning
            /////////////////////

            // show message if not enough data
            var sum = orderedTimelineList.Sum(i => i.DurationInSeconds);
            if (orderedTimelineList.Count <= 3 || sum < 5 * 60) // 3 is the minimum number of input-data-items & 5 minutes of data
            {
                _sb.Append(VisHelper.NotEnoughData(Dict.NotEnoughData));
                return _sb.ToString();
            }

            // remove first + last items if IDLE
            if (orderedTimelineList.First().ActivityCategory == ActivityCategory.Idle)
            {
                orderedTimelineList.Remove(orderedTimelineList.First());
            }
            if (orderedTimelineList.Last().ActivityCategory == ActivityCategory.Idle)
            {
                orderedTimelineList.Remove(orderedTimelineList.Last());
            }

            /////////////////////
            // Create HTML
            /////////////////////

            _sb.Append(GetActivityVisualizationContent(orderedTimelineList));
            return _sb.ToString();

            //var data = Queries.GetWindowTitles(30, _date);

            //foreach(List<string> list in data)
            //{
            //    foreach(string title in list)
            //    {
            //        html += title + " ";
            //    }
            //}
            //return html;

        }


        private string GetActivityVisualizationContent(List<WindowsActivity> activityList)
            {
                var categories = activityList.Select(a => a.ActivityCategory).Distinct().OrderBy(a => a).ToList();
                const string windowTitleTimeline = "WindowTitleTimeline";
                const string defaultHoverText = "Hint: Hover over the timeline to see details.";

                var html = string.Empty;

                /////////////////////
                // CSS
                /////////////////////

                html += @"<style type='text/css'>
                    .axis path,
                    .axis line {
                      fill: none;
                      stroke: black;
                      shape-rendering: crispEdges;
                    }
                    .axis text {
                      font-size: .71em;
                    }
                    .timeline-label {
                      font-size: .71em;
                    }
                    /*.coloredDiv {
                      height:20px; width:20px; float:left; margin-right:5px;
                    }
                    </style>";

                /////////////////////
                // Javascript
                /////////////////////

                html += @"<script type='text/javascript'>
                function loadWindowTitleTimeline(){ ";

                // create formatted javascript data list
                html += "var data2 = [" + CreateJavascriptActivityDataList(activityList) + "]; ";

                // create color scale
                // html += CreateColorScheme(categories);

                // width & height
                html += "var itemWidth = 0.98 * document.getElementsByClassName('item Wide')[0].offsetWidth;";
                html += "var itemHeight = 0.13 * document.getElementsByClassName('item Wide')[0].offsetHeight;";

                // hover Event (d: current rendering object, i: index during d3 rendering, data: data object)
                const string hover = @".hover(function(d, i, data2) { 
                                    document.getElementById('hoverDetailsWT').innerHTML = '<span style=\'font-size:1.2em; color:#007acc;\'>Title: ' + d['title'] + ' </span>'
                                })";

                // mouseout Event
                const string mouseout = @".mouseout(function (d, i, datum) { document.getElementById('hoverDetailsWT').innerHTML = '" + defaultHoverText + "'; })";

                // define configuration
                html += "var " + windowTitleTimeline + " = d3.timeline().width(" + TimelineZoomFactor + " * itemWidth).itemHeight(itemHeight)" + hover + mouseout + ";"; // .colors(colorScale).colorProperty('activity') // .stack()
                html += "var svg = d3.select('#" + windowTitleTimeline + "').append('svg').attr('width', itemWidth).datum(data2).call(" + windowTitleTimeline + "); ";

                html += "}; "; // end #1
                html += "</script>";

                /////////////////////
                // HTML
                /////////////////////

                // show details on hover
                html += "<div style='height:37%; style='align: center'><p id='hoverDetailsWT'>" + defaultHoverText + "</p></div>";

                // add timeline
                html += "<div id='" + windowTitleTimeline + "' align='center'></div>";

                // add legend 
                // html += GetLegendForCategories(categories);

                return html;
            }

            /// <summary>
            /// prepares a formatted javascript list of the window titles
            /// </summary>
            /// <param name="activityList"></param>
            /// <returns></returns>
            private string CreateJavascriptActivityDataList(List<WindowsActivity> activityList)
            {
                var html = string.Empty;

                var categories = activityList.Select(a => a.ActivityCategory).Distinct().ToList();

                string col;
                Random rnd = new Random();
                var times = new StringBuilder();
                foreach (var activityEntry in activityList)
                {
                    List<WindowProcessItem> windowList = activityEntry.WindowProcessList;

                    foreach (var i in windowList)
                    {
                        int num = rnd.Next(1, 13);
                        col = GetHtmlColorForContextCategory(num, activityEntry.ActivityCategory);
 
                        var startTime = JavascriptTimestampFromDateTime(activityEntry.StartTime);
                        var endTime = JavascriptTimestampFromDateTime(activityEntry.EndTime);

                        // add data used for the timeline and the timeline hover
                        times.Append("{");
                        times.Append("'starting_time': "); times.Append(startTime);
                        times.Append(", 'ending_time': "); times.Append(endTime);
                        times.Append(", 'color': "); times.Append(col);
                        times.Append(", 'title': '"); times.Append(i.WindowTitle);
                        times.Append("'}, ");

                    }
                }

                html += "{times: [" + times.ToString() + "]}, ";

                return html;
            }


            

            #region Helpers for legend and colors


            /// <summary>
            /// Return a random color for each context category (temporary implementation)
            /// </summary>
            /// <param name="category"></param>
            /// <param name="num"></param>
            /// <returns></returns>
            private static string GetHtmlColorForContextCategory(int num, ActivityCategory category)
            {
                const string noneColor = "#DDDDDD";

                if (category == ActivityCategory.Idle)
                {
                    return "'white'";
                }
                switch (num)
                {
                    case 1:
                        return "'#A547D1'";//"#6B238E";//"#00b300"; //"#007acb"; // darker blue
                    case 2:
                        return "'#C91BC7'";//"#8EC4E8"; // fairest blue
                    case 3:
                        return "'#D7ADEB'"; //"#1484CE"; //fairest blue
                    case 4:
                        return "'#F9D1F8'";// "#1484CE"; //fairest blue  
                    case 5:
                    case 6:
                        return "'#99EBFF'";//#87CEEB";// "#3258E6";//  "#00b300"; // dark green
                    case 7:
                    case 8:
                        return "'#12A5F4'";// "#C91BC7";//"#00cc00"; // fair green
                    case 9:
                        return "'#9DB7E8'"; // "#F9D1F8";//"#e855e8"; // dark violett
                    case 10:
                        return "'#326CCC'";// "#2858a5";//"#ED77EB"; // fair violett
                                           //case ActivityCategory.WebBrowsing:
                    case 11:
                        return "'#FF9333'"; //orange "#FFE3CB";//"#FFA500"; 
                    case 12:
                        return "'#FFC999'"; // "red"; // fair orange
                    case 13:
                        return "#'d3d3d3'"; // light gray
                    case 14:
                    case 15:
                    case 16:
                        return "gray";
                    case 17:
                        return "white";
                    case 18:
                        return noneColor;
                }
                return noneColor; // default color
            }


        #endregion

        #region Other Helpers


        private static long JavascriptTimestampFromDateTime(DateTime date)
            {
                var datetimeMinTimeTicks = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks;
                return ((date.ToUniversalTime().Ticks - datetimeMinTimeTicks) / 10000);
                // return (date.Ticks - 621355968000000000)/10000; //old: had wrong timezone
            }

            #endregion
        }
    }


