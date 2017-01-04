// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-03
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Shared;
using Shared.Helpers;
using WindowsActivityTracker.Data;
using System.Globalization;
using WindowsActivityTracker.Models;

namespace WindowsActivityTracker.Visualizations
{
    internal class DayFragmentationTimeline : BaseVisualization, IVisualization
    {
        private readonly DateTimeOffset _date;
        private bool _isStacked = false; //TODO: test with stack
        private const int _visWidth = 800; //1200;
        private const bool _mapToActivity = true;

        public DayFragmentationTimeline(DateTimeOffset date)
        {
            this._date = date;

            Title = "Activities over the Day"; //hint; overwritten below
            IsEnabled = true; //todo: handle by user
            Order = 2; //todo: handle by user
            Size = VisSize.Wide;
            Type = VisType.Day;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            /////////////////////
            // fetch data sets
            /////////////////////
            var orderedTimelineList = Queries.GetDayTimelineData(_date, _mapToActivity);

            /////////////////////
            // data cleaning
            /////////////////////
            
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
            // HTML
            /////////////////////

            html += GetActivityVisualizationContent(orderedTimelineList);

            /////////////////////
            // JS
            /////////////////////


            return html;
        }

        private string GetActivityVisualizationContent(List<WindowsActivity> activityList)
        {
            var categories = activityList.Select(a => a.ActivityCategory).Distinct().OrderBy(a => a).ToList();

            // TODO: remove these
            string activityTimeline = "activityTimeline";
            string onLoad = "onLoad";

            var html = string.Empty;

            /////////////////////
            // Javascript
            /////////////////////

            html += @"<script type='text/javascript'>
                                var " + onLoad + @" = window.onload;
                                window.onload = function() { 
                                if (typeof " + onLoad + @" == 'function') { " + onLoad + "(); } ";

            // create formatted javascript data list
            html += "var data = [" + CreateJavascriptActivityDataList(activityList) + "]; ";

            // create color scale
            html += CreateColorScheme(categories);

            // define configuration
            html += (_isStacked)
                    ? "var " + activityTimeline + " = d3.timeline().width(" + _visWidth + ").stack().colors(colorScale).colorProperty('activity');"
                    : "var " + activityTimeline + " = d3.timeline().colors(colorScale).colorProperty('activity');";
            html += "var svg = d3.select('#" + activityTimeline + "').append('svg').attr('width', " + _visWidth + ").datum(data).call(" + activityTimeline + "); ";

            html += "}; "; // end #1
            html += "</script>";

            /////////////////////
            // HTML
            /////////////////////
            html += "<div id='" + activityTimeline + "' align='center'></div>";

            html += GetLegendForCategories(_visWidth, categories);

            return html;
        }

        /// <summary>
        /// prepares a formatted javascript list of the participantActivityData
        /// </summary>
        /// <param name="participantActivityData"></param>
        /// <returns></returns>
        private string CreateJavascriptActivityDataList(List<WindowsActivity> activityList)
        {
            var html = string.Empty;

            var categories = activityList.Select(a => a.ActivityCategory).Distinct().ToList();

            foreach (var category in categories)
            {
                var times = string.Empty;
                foreach (var activityEntry in activityList.Where(a => a.ActivityCategory == category))
                {
                    var startTime = JavascriptTimestampFromDateTime(activityEntry.StartTime);
                    var endTime = JavascriptTimestampFromDateTime(activityEntry.EndTime);
                    times += "{'starting_time': " + startTime + ", 'ending_time': " + endTime + "}, ";
                }

                html += (_isStacked)
                    ? "{label: '" + category + "', activity: '" + category + "', times: [" + times + "]}, "
                    : "{activity: '" + category + "', times: [" + times + "]}, ";
            }

            return html;
        }

        /// <summary>
        /// Creates a colored square for each category (legend)
        /// </summary>
        /// <param name="activityDataSet"></param>
        /// <param name="visWidth"></param>
        /// <returns></returns>
        private string GetLegendForCategories(int visWidth, List<ActivityCategory> categoryList)
        {
            var html = string.Empty;
            html += "<style type='text/css'>";
            html += "#legend li { display:inline-block; padding-right: 20px; list-style-type: square; }";
            html += "li:before { content: '■ '} ";
            html += "li span { font: 10px sans-serif; color: black; } ";
            html += "</style>";

            html += categoryList.Aggregate(string.Empty, (current, cat) => current + ("<li style='color:" + GetHtmlColorForContextCategory(cat) + "'><span>" + GetDescriptionForContextCategory(cat) + "</span></li>"));

            // order activity list alphabetically, for a better legend!
            //activityList.Sort((actOne, actTwo) => GetDescriptionForContextCategory(actOne).CompareTo(GetDescriptionForContextCategory(actTwo)));

            // first (if existent) put development related, then reading/editing, then meetings, then browsing, then all others in new line!
            //var devActivities = activityList.Where(activity => IsDev(activity));
            //var edActivities = activityList.Where(activity => IsEditing(activity));
            //var commActivities = activityList.Where(activity => IsCommunication(activity));
            //var browsActivities = activityList.Where(activity => IsBrowsing(activity));
            //var otherActivities = activityList.Where(activity => !IsDev(activity) && !IsEditing(activity) && !IsCommunication(activity) && !IsBrowsing(activity));

            //html += activityList.Aggregate(string.Empty, (current, item) => current + ("<li style='color:" + GetHtmlColorForContextCategory(item) + "'><span>" + GetDescriptionForContextCategory(item) + "</span></li>"));

            //html += "<style>tab1 {padding-left:6.4em} tab2 {padding-left:5.4em} tab3 {padding-left:0.9em} tab4 {padding-left:8.2em} tab5 {padding-left:9.6em} tab6 {padding-left:4em}</style>"
            //        + "<div style='width:" + visWidth + "px; alignment:center;'><ul id='legend' align='left' list-style-type:none>"
            //        //order colors so that darker one comes first then aggregate all activities
            //        + ((devActivities != null && devActivities.Count() > 0) ? "<ul id='legend' >Development: <tab1>" + devActivities.OrderBy(activity => GetHtmlColorForContextCategory(activity)).Aggregate(string.Empty, (current, item) => current + (GetLegendEntryForActivity(item))) + "</tab1></ul><ul id='legend'>" : String.Empty)
            //        + ((edActivities != null && edActivities.Count() > 0) ? "Editing/Reading: <tab2>  " + edActivities.Aggregate(string.Empty, (current, item) => current + (GetLegendEntryForActivity(item))) + "</tab2></ul><ul id='legend' align='center'>" : String.Empty)
            //        + ((commActivities != null && commActivities.Count() > 0) ? "Meetings/Comm./Planning: <tab3>" + commActivities.OrderBy(activity => GetHtmlColorForContextCategory(activity)).Aggregate(string.Empty, (current, item) => current + (GetLegendEntryForActivity(item))) + "</tab3></ul><ul id='legend' align='center'>" : String.Empty)
            //        + "Meetings (Outlook): <tab6>" + GetLegendEntryForActivity("PlannedMeeting") + "</tab6></ul><ul id='legend' align='center'>"
            //        + ((browsActivities != null && browsActivities.Count() > 0) ? "Browsing:  <tab4>   " + browsActivities.OrderBy(activity => GetHtmlColorForContextCategory(activity)).Aggregate(string.Empty, (current, item) => current + (GetLegendEntryForActivity(item))) + "</tab4></ul><ul id='legend' align='center'>" : String.Empty)
            //        + ((otherActivities != null && otherActivities.Count() > 0) ? "Other: <tab5>  " + otherActivities.Aggregate(string.Empty, (current, item) => current + (GetLegendEntryForActivity(item))) + "</tab5></ul><ul id='legend' align='center'>" : String.Empty)
            //        + "</ul></div>";

            return html;
        }

        private string GetLegendEntryForActivity(ActivityCategory category)
        {
            return "<li style='color:" + GetHtmlColorForContextCategory(category) + "'><span>" + GetDescriptionForContextCategory(category) + "</span></li>";
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        /// <param name="activityList"></param>
        /// <returns></returns>
        private string CreateColorScheme(List<ActivityCategory> categories)
        {
            var rangeString = categories.Aggregate(string.Empty, (current, item) => current + ("'" + GetHtmlColorForContextCategory(item) + "', "));
            var activityString = categories.Aggregate(string.Empty, (current, item) => current + ("'" + item + "', "));

            var html = "var colorScale = d3.scale.ordinal().range([" + rangeString + "]).domain([" + activityString + "]); ";

            return html;
        }

        /// <summary>
        /// Return a color for each context category
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        private string GetHtmlColorForContextCategory(ActivityCategory category)
        {
            var noneColor = "#DDDDDD";

            switch (category)
            {
                case ActivityCategory.DevCode:
                    return "#A547D1";//"#6B238E";//"#00b300"; //"#007acb"; // darker blue
                case ActivityCategory.DevDebug:
                    return "#C91BC7";//"#8EC4E8"; // fairest blue
                case ActivityCategory.DevReview:
                    return "#D7ADEB"; //"#1484CE"; //fairest blue
                case ActivityCategory.DevVc:
                    return "#F9D1F8";// "#1484CE"; //fairest blue   !!!
                case ActivityCategory.ReadWriteDocument:
                //case ActivityCategory.ManualEditor:
                //return "#00cc00";//"#36c1c4"; // another blue
                case ActivityCategory.PlannedMeeting:
                    return "#99EBFF";//#87CEEB";// "#3258E6";//  "#00b300"; // dark green
                case ActivityCategory.InformalMeeting:
                case ActivityCategory.InstantMessaging:
                    return "#12A5F4";// "#C91BC7";//"#00cc00"; // fair green
                case ActivityCategory.Planning:
                    return "#9DB7E8"; // "#F9D1F8";//"#e855e8"; // dark violett
                case ActivityCategory.Email:
                    return "#326CCC";// "#2858a5";//"#ED77EB"; // fair violett
                //case ActivityCategory.WebBrowsing:
                case ActivityCategory.WorkRelatedBrowsing:
                    return "#FF9333"; //orange "#FFE3CB";//"#FFA500"; 
                case ActivityCategory.WorkUnrelatedBrowsing:
                    return "#FFC999"; // "red"; // fair orange
                case ActivityCategory.FileNavigationInExplorer:
                    return "#d3d3d3"; // light gray
                case ActivityCategory.Other:
                case ActivityCategory.OtherRdp:
                //case ActivityCategory.OtherMusic:
                case ActivityCategory.Unknown:
                    return "gray";
                case ActivityCategory.Idle:
                    return "white";
                case ActivityCategory.Uncategorized:
                    return noneColor; // TODO: check
            }

            return noneColor; //"#007acb"; // default color
        }

        /// <summary>
        /// Return a screen name for the activity category
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        private string GetDescriptionForContextCategory(ActivityCategory category)
        {
            switch (category)
            {
                case ActivityCategory.DevCode:
                    return "Development (Coding)";
                case ActivityCategory.DevDebug:
                    return "Debugging";
                case ActivityCategory.DevVc:
                    return "Version Control";
                case ActivityCategory.DevReview:
                    return "Reviewing";
                case ActivityCategory.ReadWriteDocument:
                    return "Reading/Editing documents";
                case ActivityCategory.InformalMeeting:
                case ActivityCategory.InstantMessaging:
                    return "Ad-hoc meetings / IM";
                case ActivityCategory.PlannedMeeting:
                    return "Scheduled meetings/calls";
                case ActivityCategory.Planning:
                    return "Planning tasks/work items";
                case ActivityCategory.Email:
                    return "Reading/writing emails";
                //case ActivityCategory.WebBrowsing:
                //    return "Browsing (uncategorized)";
                case ActivityCategory.WorkRelatedBrowsing:
                    return "Browsing (categorized as work related)";// "Work related browsing";
                case ActivityCategory.WorkUnrelatedBrowsing:
                    return "Browsing (categorized as less work related)";// "Work un-related browsing";
                case ActivityCategory.FileNavigationInExplorer:
                    return "Navigation in File Explorer";
                case ActivityCategory.Other:
                    return "Other";
                //case ActivityCategory.OtherMusic:
                //    return "OtherMusic";
                case ActivityCategory.Unknown:
                    return "Uncategorized activities";
                case ActivityCategory.OtherRdp:
                    return "RDP (uncategorized)";
                case ActivityCategory.Idle:
                    return "Idle (e.g. break, lunch)";
            }

            return "??"; // default color
        }
  

        #region Helpers

        private static long JavascriptTimestampFromDateTime(DateTime date)
        {
            var datetimeMinTimeTicks = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks;
            return ((date.ToUniversalTime().Ticks - datetimeMinTimeTicks) / 10000);
            // return (date.Ticks - 621355968000000000)/10000; //old: had wrong timezone
        }

        #endregion
    }
}
