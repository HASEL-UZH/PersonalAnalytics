// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Shared;
using Shared.Data;

namespace PersonalAnalytics.Visualizations
{
    class ContextTimelineChart : IChart
    {
        private readonly DateTimeOffset _date;

        public ContextTimelineChart(DateTimeOffset date)
        {
            this._date = date;
        }

        public string GetHtml()
        {
            // calculate maxWidth
            // TODO: do later

            // produce visualizations (html)
            var html = string.Empty;
            html += VisHelper.ChartTitle("Timeline of your Context, User Input and Perceived Productivity");
            html += CreateContextGantTimeline();
            html += CreateUserInputLineGraph();
            html += CreateProdTasksLineGraph();
            return html;
        }

        #region Context Timeline Visualization

        private string CreateContextGantTimeline()
        {
            var html = string.Empty;

            // fetch raw data sets
            var activityDataSet = new List<ActivitiesDto>();
            activityDataSet = Database.GetInstance().GetActivitiesTimelineData(_date);

            var taskDataSet = new Dictionary<string, List<StartEndTimeDto>>();
            taskDataSet = Database.GetInstance().GetTaskGantTimelineData(_date);


            // merge with remote data if necessary TODO: finish!
//            tasksWorkedOnDataSet = RemoteDataHandler.VisualizeWithRemoteData()
//                ? RemoteDataHandler.MergeTasksData(tasksWorkedOnDataLocal, Database.GetInstanceRemote().GetTasksWorkedOnData(_date))
//                : tasksWorkedOnDataLocal;

            Console.WriteLine(taskDataSet);
            // check data available
            if (activityDataSet == null || activityDataSet.Count <= 2)
            {
                html += VisHelper.NotEnoughData(Dict.NotEnoughDataMiniSurvey);
                return html;
            }

            /////////////////////
            // Some strings for the attributes
            /////////////////////
            const string contextGantTimeline = "contextGantTimeline";
            const string labelData = "labelData";
            //const string hoverRes = "hoverRes";
            //const string coloredDiv = "coloredDiv";
            //const string hoverDetails = "hoverDetails";
            const int visWidth = 850; //1200;

            /////////////////////
            // JS
            /////////////////////
            html += "<script type='text/javascript'>";
            html += "var oldOnload2 = window.onload;";
            html += "window.onload = function() { "; // start #1
            html += "if (typeof oldOnload2 == 'function') { oldOnload2(); } ";

            // create formatted javascript data list
            html += "var " + labelData + " = [" + CreateJavascriptDataList(taskDataSet, activityDataSet) + "]; ";

            // define configuration
            // hint: d is the current rendering object, i is the index during d3 rendering, datum is the id object
            //const string hoverFunction = "function (d, i, datum) { var div = $('#" + hoverRes + "'); var colors = " + contextGantTimeline + ".colors(); div.find('." + coloredDiv + "').css('background-color', colors(i)); div.find('#" + hoverDetails + "').text(datum.label + ' from: ' + d.starting_time + ', to: ' + d.ending_time); }";
            html += "var " + contextGantTimeline + " = d3.timeline().width(" + visWidth + ").stack().margin({left: 140, right: 1, top: -20, bottom: 0});"; //.hover(" + hoverFunction + ");"; //TODO: commented out hover stuff
            html += "var svg = d3.select('#" + contextGantTimeline + "').append('svg').attr('width', " + visWidth + ").datum(" + labelData + ").call(" + contextGantTimeline + "); ";

            html += "}; "; // end #1
            html += "</script>";

            /////////////////////
            // HTML
            /////////////////////
            html += "<div id='" + contextGantTimeline + "' align='right'></div>";
            //html += "<div id='" + hoverRes + "'><div class='" + coloredDiv + "'></div><div id='" + hoverDetails + "'></div>" + "</div>";

            html += GetLegendForCategories(activityDataSet, visWidth);

            return html;
        }

        /// <summary>
        /// Creates a colored square for each category (legend)
        /// </summary>
        /// <param name="activityDataSet"></param>
        /// <param name="visWidth"></param>
        /// <returns></returns>
        private static string GetLegendForCategories(List<ActivitiesDto> activityDataSet, int visWidth)
        {
            var html = string.Empty;
            html += "<style type='text/css'>";
            html += "#legend li { display:inline-block; padding-right: 20px; list-style-type: square; }";
            html += "li:before { content: '■ '} ";
            html += "li span { font: 10px sans-serif; color: black; } ";
            html += "</style>";

            var categoryList = new List<ContextCategory>();

            foreach (var item in activityDataSet)
            {
                if (categoryList.Contains(item.Context)) continue;
                categoryList.Add(item.Context);
            }

            html += "<div style='width:" + visWidth + "px'><ul id='legend' align='center'>"
                    + categoryList.Aggregate(string.Empty, (current, item) => current + ("<li style='color:" + GetHtmlColorForContextCategory(item) + "'><span>" + GetDescriptionForContextCategory(item) + "</span></li>")) 
                    + "</ul></div>";

            return html;
        }

        /// <summary>
        /// merges the two data lists (task & activity) and create a data set (javascript formatted)
        /// </summary>
        /// <param name="taskDataSet"></param>
        /// <param name="activityDataSet"></param>
        /// <returns></returns>
        private static string CreateJavascriptDataList(Dictionary<string, List<StartEndTimeDto>> taskDataSet, List<ActivitiesDto> activityDataSet)
        {
            var html = string.Empty;
            foreach (var task in taskDataSet)
            {
                var startEndTimeList = "";

                // have a default gray background (basis gantt chart item)
                foreach (var taskTimes in task.Value)
                {
                    startEndTimeList += " {'starting_time': " + taskTimes.StartTime + ", 'ending_time': " + taskTimes.EndTime +
                                        ", 'color': '" + NoneColor + "'}, ";
                }

                // now overlay detailed info from activities
                foreach (var activity in activityDataSet)
                {
                    foreach (var taskTimes in task.Value)
                    {
                        if (activity.StartTime >= taskTimes.StartTime && activity.StartTime < taskTimes.EndTime
                            && activity.EndTime > taskTimes.StartTime && activity.EndTime <= taskTimes.EndTime)
                        {
                            startEndTimeList += " {'starting_time': " + activity.StartTime + ", 'ending_time': " +
                                                activity.EndTime +
                                                ", 'color': '" + GetHtmlColorForContextCategory(activity.Context) + "'}, ";
                        }
                    }
                }
                html += " {label: '" + task.Key.Replace("'", "\\'") + "', times: [" + startEndTimeList + "]}, ";
            }
            return html;
        }

        private const string NoneColor = "#DDDDDD";
        /// <summary>
        /// Return a color for each context category
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        private static string GetHtmlColorForContextCategory(ContextCategory category)
        {
            switch (category)
            {
                case ContextCategory.DevCode:
                    return "#007acb"; // darker blue
                case ContextCategory.DevDebug:  
                    return "#8EC4E8"; // fairest blue
                case ContextCategory.DevReview:
                    return "#1484CE"; //fairest blue
                case ContextCategory.ReadWriteDocument:
                    return "#36c1c4"; // another blue
                case ContextCategory.PlannedMeeting:
                    return "#00b300"; // dark green
                case ContextCategory.InformalMeeting:
                    return "#00cc00"; // fair green
                case ContextCategory.Planning:
                    return "#e855e8"; // dark violett
                case ContextCategory.Email:
                    return "#f198f1"; // fair violett
                case ContextCategory.WorkRelatedBrowsing:
                    return "#FF7F0E"; //orange
                case ContextCategory.WorkUnrelatedBrowsing:
                    return "#FFBB78"; // fair orange
                case ContextCategory.Other:
                    return "gray";
                case ContextCategory.None:
                    return NoneColor;
            }

            return NoneColor; //"#007acb"; // default color
        }

        /// <summary>
        /// Return a screen name for the activity category
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        private static string GetDescriptionForContextCategory(ContextCategory category)
        {
            switch (category)
            {
                case ContextCategory.DevCode:
                    return "Development (Coding)";
                case ContextCategory.DevDebug:
                    return "Debugging";
                case ContextCategory.DevReview:
                    return "Reviewing";
                case ContextCategory.ReadWriteDocument:
                    return "Reading/Editing documents";
                case ContextCategory.InformalMeeting:
                    return "Ad-hoc meetings";
                case ContextCategory.PlannedMeeting:
                    return "Scheduled meetings/calls";
                case ContextCategory.Planning:
                    return "Planning tasks/work items";
                case ContextCategory.Email:
                    return "Reading/writing emails";
                case ContextCategory.WorkRelatedBrowsing:
                    return "Work related browsing";
                case ContextCategory.WorkUnrelatedBrowsing:
                    return "Work un-related browsing";
                case ContextCategory.Other:
                    return "Uncategorized activities";
                case ContextCategory.None:
                    return "Offline (e.g. break, lunch)";
            }

            return "??"; // default color
        }

        #endregion

        #region User Input Timeline

        /// <summary>
        /// Creates a line graph showing the user input level
        /// </summary>
        /// <returns></returns>
        private string CreateUserInputLineGraph()
        {
            var html = string.Empty;

            // fetch data sets
            var userInputDataLocal = Database.GetInstance().GetUserInputTimelineData(_date);

            if (userInputDataLocal == null || userInputDataLocal.Count < 3) // 3 is the minimum number of input-data-items
            {
                html += VisHelper.NotEnoughData(Dict.NotEnoughDataMiniSurvey);
                return html;
            }

            /////////////////////
            // Some strings for the attributes
            /////////////////////
            const string userInputLineGraph = "userInputLineGraph";
            const int width = 770;
            const int height = 150;

            /////////////////////
            // HTML
            /////////////////////
            html += "<div id='" + userInputLineGraph + "' align='right'></div>";

            /////////////////////
            // JS
            /////////////////////
            var ticks = CalculateLineChartAxisTicks(_date);
            var timeAxis = userInputDataLocal.Aggregate("", (current, a) => current + (Helpers.JavascriptTimestampFromDateTime(a.Key) + ", ")).Trim().TrimEnd(',');
            var userInputFormattedData = userInputDataLocal.Aggregate("", (current, p) => current + (p.Value + ", ")).Trim().TrimEnd(',');

            const string colors = "'User_Input_Level' : '#007acb'";
            var data = "x: 'timeAxis', columns: [['timeAxis', " + timeAxis + "], ['User_Input_Level', " + userInputFormattedData + " ] ], type: 'spline', colors: { " + colors + " }, axis: { 'PerceivedProductivity': 'y' }";
            var size = "width: " + width + ", height: " + height;
            const string legend = "show: true"; // can only be shown on the right side, and bottom (not left) ... :(
            const string tooltip = "show: false";
            const string point = "show: false";
            var axis = "x: { localtime: true, type: 'timeseries', tick: { values: [ " + ticks + "], format: function(x) { return formatDate(x.getHours()); }}  }, y: { show: true, min: 0 }";
            var parameters = " bindto: '#" + userInputLineGraph + "', padding: { right: -10 }, data: { " + data + " }, legend: { " + legend + " }, axis: { " + axis + " } , size: { " + size + " }, tooltip: { " + tooltip + " }, point: { " + point + " }";
            // padding: { left: 0px, right: 0px }, 

            html += "<script type='text/javascript'>";
            html += "var formatDate = function(hours) { var suffix = 'AM'; if (hours >= 12) { suffix = 'PM'; hours = hours - 12; } if (hours == 0) { hours = 12; } if (hours < 10) return '0' + hours + ' ' + suffix; else return hours + ' ' + suffix; };";
            html += "var " + userInputLineGraph + " = c3.generate({ " + parameters + " });"; // return x.getHours() + ':' + x.getMinutes();
            html += "</script>";

            return html;
        }

        /// <summary>
        /// Creates a list of one-hour axis times
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private static string CalculateLineChartAxisTicks(DateTimeOffset date)
        {
            var dict = new Dictionary<DateTime, int>();
            Database.GetInstance().PrepareTimeAxis(date, dict, 60);

            return dict.Aggregate("", (current, a) => current + (Helpers.JavascriptTimestampFromDateTime(a.Key) + ", ")).Trim().TrimEnd(',');
        }

        #endregion

        #region Tasks & Perceived Productivity Visualization

        private string CreateProdTasksLineGraph()
        {
            var html = string.Empty;

            // fetch data sets
            var productivityDataSet = new List<ProductivityTimeDto>();
            var tasksWorkedOnDataSet = new List<TasksWorkedOnTimeDto>();
            var tasksWorkedOnDataLocal = Database.GetInstance().GetTasksWorkedOnData(_date);
            var productivityDataLocal = Database.GetInstance().GetUserProductivityData(_date, true);

            // merge with remote data if necessary
            tasksWorkedOnDataSet = RemoteDataHandler.VisualizeWithRemoteData()
                ? RemoteDataHandler.MergeTasksData(tasksWorkedOnDataLocal, Database.GetInstanceRemote().GetTasksWorkedOnData(_date))
                : tasksWorkedOnDataLocal;

            productivityDataSet = RemoteDataHandler.VisualizeWithRemoteData()
                ? RemoteDataHandler.MergeProductivityData(productivityDataLocal, Database.GetInstanceRemote().GetUserProductivityData(_date, false))
                : productivityDataLocal;

            if (tasksWorkedOnDataSet == null || tasksWorkedOnDataSet.Count <= 1 ||
                productivityDataSet == null || productivityDataSet.Count <= 1)
            {
                html += VisHelper.NotEnoughData(Dict.NotEnoughDataMiniSurvey);
                return html;
            }

            /////////////////////
            // Some strings for the attributes
            /////////////////////
            const string prodTasksLineGraph = "prodTasksLineGraph";
            const int width = 730; //780;
            const int height = 130;

            /////////////////////
            // CSS
            /////////////////////
            html += "<style type='text/css'>";
            html += ".c3-line { stroke-width: 2px; }";
            //html += ".c3-axis-y-label { color: #007acc; fill: #007acc; font-size: 1.4em; }"; // left axis label color
            //html += ".c3-axis-y2-label { fill: 'gray'; font-size: 1.4em; }"; // right axis label color
            html += "</style>";

            /////////////////////
            // HTML
            /////////////////////
            html += "<div id='" + prodTasksLineGraph + "' align='right'></div>";

            /////////////////////
            // JS
            /////////////////////
            var ticks = CalculateLineChartAxisTicks(_date);
            var timeAxis = tasksWorkedOnDataSet.Aggregate("", (current, a) => current + (a.Time + ", ")).Trim().TrimEnd(',');
            var productivityFormattedData = productivityDataSet.Aggregate("", (current, p) => current + (p.UserProductvity + ", ")).Trim().TrimEnd(',');
            var tasksFormattedData = tasksWorkedOnDataSet.Aggregate("", (current, t) => current + (t.TasksWorkedOn + ", ")).Trim().TrimEnd(',');

            const string colors = "'Number_Of_Activities_In_Session': '#007acc', 'Perceived_Productivity': 'gray'";
            var data = "x: 'timeAxis', columns: [['timeAxis', " + timeAxis + "], ['Number_Of_Activities_In_Session',  " + tasksFormattedData + " ], ['Perceived_Productivity', " + productivityFormattedData + " ] ], type: 'spline', colors: { " + colors + " } , axes: { 'PerceivedProductivity': 'y', 'NumberOfTasksWorkedOn': 'y2' }";
            var size = "width: " + width + ", height: " + height;
            const string tooltip = "show: false"; //(tasksWorkedOnDataSet.Count > 4) ? " show: false " : " show: true "; // only hide if there are more than 4 elements
            const string legend = "show: true"; // can only be shown on the right side, and bottom (not left) ... :(
            const string point = "show: false";

            //var axis = "x: { localtime: true, type: 'timeseries', tick: { values: [ " + ticks + "],  format: function(x) { return pad2(x.getHours()) + ':' + pad2(x.getMinutes()); }}  }, y: { max: 7, min: 1, label: { text: 'Perceived Productivity', position: 'outer-right' } }, y2: { show: true, min: 0, label: { text: 'Number of tasks worked on', position: 'outer-right' } }";
            var axis = "x: { localtime: true, type: 'timeseries', tick: { values: [ " + ticks + "], format: function(x) { return formatDate(x.getHours()); }}  }, y: { show: false, max: 7, min: 1 }, y2: { show: false, min: 0 }";
            var parameters = " bindto: '#" + prodTasksLineGraph + "', data: { " + data + " }, legend: { " + legend + " }, axis: { " + axis + " } , size: { " + size + " }, tooltip: { " + tooltip + " }, point: { " + point + " } ";

            html += "<script type='text/javascript'>";
            html += "var formatDate = function(hours) { var suffix = 'AM'; if (hours >= 12) { suffix = 'PM'; hours = hours - 12; } if (hours == 0) { hours = 12; } if (hours < 10) return '0' + hours + ' ' + suffix; else return hours + ' ' + suffix; };";
            html += "var " + prodTasksLineGraph + " = c3.generate({ " + parameters + " });"; // return x.getHours() + ':' + x.getMinutes();
            html += "</script>";

            return html;
        }

        #endregion
    }
}
