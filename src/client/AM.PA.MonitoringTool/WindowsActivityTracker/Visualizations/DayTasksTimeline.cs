// Created by Patrick Gousseau (pcgousseau@gmail.com) from the University of British Columbia
// Created: 2021-08-20

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
using System.Timers;
using WindowsActivityTracker.TaskDetection;
using Task = WindowsActivityTracker.TaskDetection.Task;
using Fernandezja.ColorHashSharp;

namespace WindowsActivityTracker.Visualizations
    {
        internal class DayTasksTimeline : BaseVisualization, IVisualization
        {
            private readonly DateTimeOffset _date;
            private const int TimelineZoomFactor = 1; // shouldn't be 0!, if > 1 then the user can scroll on top of the timeline
            private StringBuilder _sb = new StringBuilder();
            private static Summarizer summarizer;
            private static DateTime startDate;
            private static int numSecs = 120; // Number of seconds per block of time
            private static double cosineSimilarity = 0.5;
       
            public DayTasksTimeline(DateTimeOffset date)
            {
                this._date = date;
                Title = "Timeline: Tasks over the Day";
                IsEnabled = true; //todo: handle by user
                Order = 2; //todo: handle by user
                Size = VisSize.Wide;
                Type = VisType.Day;
            }

            /// <summary>
            /// Initialize new summarizer
            /// </summary>
            /// <returns></returns>
            public static void initializeSummarizer()
            {
                summarizer = new Summarizer(numSecs, cosineSimilarity);
                startDate = DateTime.Now;
            }

            /// <summary>
            /// Calls method that updates summarizer every numSecs
            /// </summary>
            /// <returns></returns>
            public static void getData()
            {
                // Create a timer and set a two minute interval.
                Timer aTimer = new System.Timers.Timer();
                aTimer.Interval = numSecs * 1000;
                aTimer.Elapsed += OnTimedEvent;
                aTimer.AutoReset = true;
                aTimer.Enabled = true;
            }

            /// <summary>
            /// Fetches the window titles used in the last numSecs seconds
            /// </summary>
            /// <returns></returns>
            private static List<string> getRecentWindowTitles()
            {
                var windowTitleList = Queries.GetDayWindowTitleData(DateTime.Now, numSecs);
                List<string> windowTitles = new List<string>();

                foreach (WindowsActivity window in windowTitleList)
                {
                    var list = window.WindowProcessList;
                    foreach (WindowProcessItem process in list)
                    {
                        windowTitles.Add(process.WindowTitle);
                    }
                }
                return windowTitles;
            }

            /// <summary>
            /// Adds the recent window titles in the summarizer
            /// </summary>
            /// <returns></returns>
            private static void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
            {
                List<string> windowTitles = getRecentWindowTitles();
                Task task = new Task(windowTitles, 0.5);
                summarizer.addTask(task);
            }

            public override string GetHtml()
            {

                var html = string.Empty;

                /////////////////////
                // fetch data sets
                /////////////////////
                var orderedTimelineList = Queries.GetDayWindowTitleData(_date,numSecs);


                // show message if not enough data
                //var sum = orderedTimelineList.Sum(i => i.DurationInSeconds);
                //if (orderedTimelineList.Count <= 3 || sum < 5 * 60) // 3 is the minimum number of input-data-items & 5 minutes of data
                //{
                //    _sb.Append(VisHelper.NotEnoughData(Dict.NotEnoughData));
                //    return _sb.ToString();
                //}

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
                    function loadTasksTimeline(){ ";

                // create formatted javascript data list
                html += "var data2 = [" + CreateJavascriptActivityDataList(summarizer) + "]; ";

                // create color scale
                // html += CreateColorScheme(categories);

                // width & height
                html += "var itemWidth = 0.98 * document.getElementsByClassName('item Wide')[0].offsetWidth;";
                html += "var itemHeight = 0.13 * document.getElementsByClassName('item Wide')[0].offsetHeight;";

                // hover Event (d: current rendering object, i: index during d3 rendering, data: data object)
                const string hover = @".hover(function(d, i, data2) { 
                                        document.getElementById('hoverDetailsWT').innerHTML = '<span style=\'font-size:1.2em; color:#007acc;\'>From ' + d['starting_time_formatted'] + ' to ' + d['ending_time_formatted'] + ' (' + d['duration'] + 'min) </span>' +
                                                                                               '<br /><strong>Task</strong>: ' + d['title']; 

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


                return html;
            }

            /// <summary>
            /// Prepares a formatted javascript list of the tasks
            /// </summary>
            /// <param name="summarizer"></param>
            /// <returns></returns>
            private string CreateJavascriptActivityDataList(Summarizer summarizer)
            {
                var html = string.Empty;

                var tasks = summarizer.getTasks();
                string col;
                var times = new StringBuilder();

                foreach (Task task in tasks)
                {
                    int num = task.getTaskNum();
                    col = GetHtmlColorForContextCategory(num);

                    // Start time
                    double startTime = task.getStartTime();
                    DateTime start = startDate;           
                    start = start.AddSeconds(startTime);
                
                    // End time
                    double endTime = task.getEndTime();
                    DateTime end = startDate;
                    end = end.AddSeconds(endTime);

                    double duration = Math.Round(Convert.ToDouble((endTime - startTime) / 60),1);

                    // add data used for the timeline and the timeline hover
                    times.Append("{");
                    times.Append("'starting_time': "); times.Append(JavascriptTimestampFromDateTime(start));
                    times.Append(", 'ending_time': "); times.Append(JavascriptTimestampFromDateTime(end));
                    times.Append(", 'starting_time_formatted': '"); times.Append(start.ToString("HH:mm:ss"));
                    times.Append("', 'ending_time_formatted': '"); times.Append(end.ToString("HH:mm:ss"));
                    times.Append("', 'color': "); times.Append(col);
                    times.Append(", 'duration': "); times.Append(duration);
                    times.Append(", 'title': '"); times.Append(task.getRepresentation());
                    times.Append("'}, ");
                }

                html += "{times: [" + times.ToString() + "]}, ";
                return html;
            }

            #region Helpers for legend and colors


            /// <summary>
            /// Return a color corresponding to the task number
            /// </summary>
            /// <param name="num"></param>
            /// <returns></returns>
            private static string GetHtmlColorForContextCategory(int num)//, ActivityCategory category)
            {
                var colorHash = new ColorHash();
                string hex = "'#" + colorHash.Hex(Convert.ToString(num)) + "'";
                return hex;
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


