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

namespace WindowsActivityTracker.Visualizations
    {
        internal class DayWindowTitleList : BaseVisualization, IVisualization
        {
            private readonly DateTimeOffset _date;
            private const int TimelineZoomFactor = 1; // shouldn't be 0!, if > 1 then the user can scroll on top of the timeline
            private StringBuilder _sb = new StringBuilder();
            private static Summarizer summarizer;
            private static DateTime startDate;
       

            public DayWindowTitleList(DateTimeOffset date)
            {
                this._date = date;
                Title = "Timeline: Tasks over the Day";
                IsEnabled = true; //todo: handle by user
                Order = 2; //todo: handle by user
                Size = VisSize.Wide;
                Type = VisType.Day;

 
        }

        public static void initializeSummarizer()
        {
            summarizer = new Summarizer(30, 0.5);
            startDate = DateTime.Now;
            string filePath = @"C:\Users\pcgou\OneDrive\Documents\UBCResearch\testInterval6.txt"; // ** Modify this file path **
            File.AppendAllText(filePath, "cc" + Environment.NewLine);
                
        }

        public static void updateSummarizer()
        {
            string filePath = @"C:\Users\pcgou\OneDrive\Documents\UBCResearch\testInterval9.txt"; // ** Modify this file path **
            List<string> windowTitles = getRecentWindowTitles();
            Task task = new Task(windowTitles,0.5);
            
            summarizer.addTask(task);
            Task task1 = summarizer.getTasks()[summarizer.getTasks().Count - 1];
            File.AppendAllText(filePath, summarizer.getTasks().Count + Environment.NewLine);
            File.AppendAllText(filePath, task1.getVector()[0] + ": " + task1.getStartTime() + " - " + task1.getEndTime() + Environment.NewLine);
            List<Task> tasks = summarizer.getTasks();

            
        }

        public static void getData()
        {
            // Create a timer and set a two second interval.
            Timer aTimer = new System.Timers.Timer();
            aTimer.Interval = 30000;

            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;

            // Have the timer fire repeated events (true is the default)
            aTimer.AutoReset = true;

            // Start the timer
            aTimer.Enabled = true;
        }

        private static List<string> getRecentWindowTitles()
        {
            string filePath = @"C:\Users\pcgou\OneDrive\Documents\UBCResearch\testInterval4.txt"; // ** Modify this file path **
            var windowTitleList = Queries.GetDayWindowTitleData(DateTime.Now);

            List<string> windowTitles = new List<string>();

            foreach (WindowsActivity window in windowTitleList)
            {
                var list = window.WindowProcessList;
                foreach (WindowProcessItem process in list)
                {
                    File.AppendAllText(filePath, process.WindowTitle + Environment.NewLine);
                    windowTitles.Add(process.WindowTitle);
                }

            }
            return windowTitles;
        }

        private static void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);
            string filePath = @"C:\Users\pcgou\OneDrive\Documents\UBCResearch\testInterval2.txt"; // ** Modify this file path **

            updateSummarizer();

           // var windowTitleList = Queries.GetDayWindowTitleData(DateTime.Now);


        }


        public override string GetHtml()
            {
            // writeToFile();
            

            var html = string.Empty;

            /////////////////////
            // fetch data sets
            /////////////////////
            var orderedTimelineList = Queries.GetDayWindowTitleData(_date);
            //var orderedTimelineList = Queries.GetDayTimelineData(_date);
            /////////////////////
            // data cleaning
            /////////////////////

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


            //  return html;

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

            // add legend 
            // html += GetLegendForCategories(categories);

            return html;
        }

        /// <summary>
        /// prepares a formatted javascript list of the window titles
        /// </summary>
        /// <param name="activityList"></param>
        /// <returns></returns>
        private string CreateJavascriptActivityDataList(Summarizer summarizer)
        {
            var html = string.Empty;

            //  var categories = activityList.Select(a => a.ActivityCategory).Distinct().ToList();
            var tasks = summarizer.getTasks();
            string col;
            Random rnd = new Random();
            var times = new StringBuilder();

            foreach (Task task in tasks)
            {
                int num = task.getTaskNum();//rnd.Next(1, 13);
                col = GetHtmlColorForContextCategory(num);

                double startTime = task.getStartTime();
                DateTime start = startDate;
            
                start = start.AddSeconds(startTime);
                

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


                string filePath = @"C:\Users\pcgou\OneDrive\Documents\UBCResearch\testValues.txt"; // ** Modify this file path **
                File.AppendAllText(filePath, "start: " + start + Environment.NewLine);
                File.AppendAllText(filePath, "end: " + end + Environment.NewLine);
                File.AppendAllText(filePath, "vec: " + task.getVector()[0] + Environment.NewLine);

            }


            html += "{times: [" + times.ToString() + "]}, ";

            return html;
        }


        //private string GetActivityVisualizationContent(List<WindowsActivity> activityList)
        //{
        //    var categories = activityList.Select(a => a.ActivityCategory).Distinct().OrderBy(a => a).ToList();
        //    const string windowTitleTimeline = "WindowTitleTimeline";
        //    const string defaultHoverText = "Hint: Hover over the timeline to see details.";

        //    var html = string.Empty;

        //    /////////////////////
        //    // CSS
        //    /////////////////////

        //    html += @"<style type='text/css'>
        //            .axis path,
        //            .axis line {
        //              fill: none;
        //              stroke: black;
        //              shape-rendering: crispEdges;
        //            }
        //            .axis text {
        //              font-size: .71em;
        //            }
        //            .timeline-label {
        //              font-size: .71em;
        //            }
        //            /*.coloredDiv {
        //              height:20px; width:20px; float:left; margin-right:5px;
        //            }
        //            </style>";

        //    /////////////////////
        //    // Javascript
        //    /////////////////////

        //    html += @"<script type='text/javascript'>
        //        function loadWindowTitleTimeline(){ ";

        //    // create formatted javascript data list
        //    html += "var data2 = [" + CreateJavascriptActivityDataList(activityList) + "]; ";

        //    // create color scale
        //    // html += CreateColorScheme(categories);

        //    // width & height
        //    html += "var itemWidth = 0.98 * document.getElementsByClassName('item Wide')[0].offsetWidth;";
        //    html += "var itemHeight = 0.13 * document.getElementsByClassName('item Wide')[0].offsetHeight;";

        //    // hover Event (d: current rendering object, i: index during d3 rendering, data: data object)
        //    const string hover = @".hover(function(d, i, data2) { 
        //                            document.getElementById('hoverDetailsWT').innerHTML = '<span style=\'font-size:1.2em; color:#007acc;\'>Title: ' + d['title'] + ' </span>'
        //                        })";

        //    // mouseout Event
        //    const string mouseout = @".mouseout(function (d, i, datum) { document.getElementById('hoverDetailsWT').innerHTML = '" + defaultHoverText + "'; })";

        //    // define configuration
        //    html += "var " + windowTitleTimeline + " = d3.timeline().width(" + TimelineZoomFactor + " * itemWidth).itemHeight(itemHeight)" + hover + mouseout + ";"; // .colors(colorScale).colorProperty('activity') // .stack()
        //    html += "var svg = d3.select('#" + windowTitleTimeline + "').append('svg').attr('width', itemWidth).datum(data2).call(" + windowTitleTimeline + "); ";

        //    html += "}; "; // end #1
        //    html += "</script>";

        //    /////////////////////
        //    // HTML
        //    /////////////////////

        //    // show details on hover
        //    html += "<div style='height:37%; style='align: center'><p id='hoverDetailsWT'>" + defaultHoverText + "</p></div>";

        //    // add timeline
        //    html += "<div id='" + windowTitleTimeline + "' align='center'></div>";

        //    // add legend 
        //    // html += GetLegendForCategories(categories);

        //    return html;
        //}



        ///// <summary>
        ///// prepares a formatted javascript list of the window titles
        ///// </summary>
        ///// <param name="activityList"></param>
        ///// <returns></returns>
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
                    col = GetHtmlColorForContextCategory(num);

                    var startTime = JavascriptTimestampFromDateTime(activityEntry.StartTime);
                    var endTime = JavascriptTimestampFromDateTime(activityEntry.EndTime);

                    // add data used for the timeline and the timeline hover
                    times.Append("{");
                    times.Append("'starting_time': "); times.Append(startTime);
                    times.Append(", 'ending_time': "); times.Append(endTime);
                    times.Append(", 'color': "); times.Append(col);
                    times.Append(", 'title': '"); times.Append(i.WindowTitle);
                    times.Append("'}, ");

                    string filePath = @"C:\Users\pcgou\OneDrive\Documents\UBCResearch\testValues.txt"; // ** Modify this file path **
                    File.AppendAllText(filePath, "start: " + startTime + Environment.NewLine);
                    File.AppendAllText(filePath, "end: " + endTime + Environment.NewLine);
                  //  File.AppendAllText(filePath, "vec: " + task.getVector()[0] + Environment.NewLine);
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
        private static string GetHtmlColorForContextCategory(int num)//, ActivityCategory category)
            {
                const string noneColor = "#DDDDDD";


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


