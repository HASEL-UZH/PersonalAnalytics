// Created by André Meyer at MSR
// Created: 2015-12-14
// 
// Licensed under the MIT License.

using System;
using Shared;
using System.Collections.Generic;
using Shared.Helpers;
using System.Linq;
using TimeSpentVisualizer.Helpers;
using TimeSpentVisualizer.Models;
using System.Globalization;
using Shared.Data;
using System.Reflection;

namespace TimeSpentVisualizer.Visualizers
{
    public class TimeSpentVisualizer : BaseVisualizer
    {
        private bool _isEnabled = true;

        public TimeSpentVisualizer()
        {
            Name = "Time Spent Visualizer";
        }

        public override bool IsEnabled()
        {
            return _isEnabled;
        }

        public override string GetVersion()
        {
            var v = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;
            return VersionHelper.GetFormattedVersion(v);
        }

        public override List<IVisualization> GetVisualizationsDay(DateTimeOffset date)
        {
            var vis = new DayTimeSpentTable(date, TimeSpentShowEmailsEnabled, TimeSpentShowProgramsEnabled, TimeSpentHideMeetingsWithoutAttendeesEnabled);
            return new List<IVisualization> { vis };
        }

        private bool _timeSpentShowProgramsEnabled;
        public bool TimeSpentShowProgramsEnabled
        {
            get
            {
                _timeSpentShowProgramsEnabled = Database.GetInstance().GetSettingsBool("TimeSpentShowProgramsEnabled", false); // by default, not shown
                return _timeSpentShowProgramsEnabled;
            }
            set
            {
                var updatedIsEnabled = value;

                // only update if settings changed
                if (updatedIsEnabled == _timeSpentShowProgramsEnabled) return;

                // update settings
                Database.GetInstance().SetSettings("TimeSpentShowProgramsEnabled", value);

                // log
                Database.GetInstance().LogInfo("The participant updated the setting 'TimeSpentShowProgramsEnabled' to " + updatedIsEnabled);
            }
        }

        private bool _timeSpentShowEmailsEnabled;
        public bool TimeSpentShowEmailsEnabled
        {
            get
            {
                _timeSpentShowEmailsEnabled = Database.GetInstance().GetSettingsBool("TimeSpentShowEmailsEnabled", false); // by default, not shown
                return _timeSpentShowEmailsEnabled;
            }
            set
            {
                var updatedIsEnabled = value;

                // only update if settings changed
                if (updatedIsEnabled == _timeSpentShowEmailsEnabled) return;

                // update settings
                Database.GetInstance().SetSettings("TimeSpentShowEmailsEnabled", value);

                // log
                Database.GetInstance().LogInfo("The participant updated the setting 'TimeSpentShowEmailsEnabled' to " + updatedIsEnabled);
            }
        }

        private bool _timeSpentHideMeetingsWithoutAttendeesEnabled;
        public bool TimeSpentHideMeetingsWithoutAttendeesEnabled
        {
            get
            {
                _timeSpentHideMeetingsWithoutAttendeesEnabled = Database.GetInstance().GetSettingsBool("TimeSpentHideMeetingsWithoutAttendeesEnabled", false); // by default, shown
                return _timeSpentHideMeetingsWithoutAttendeesEnabled;
            }
            set
            {
                var updatedIsEnabled = value;

                // only update if settings changed
                if (updatedIsEnabled == _timeSpentHideMeetingsWithoutAttendeesEnabled) return;

                // update settings
                Database.GetInstance().SetSettings("TimeSpentHideMeetingsWithoutAttendeesEnabled", value);

                // log
                Database.GetInstance().LogInfo("The participant updated the setting 'TimeSpentHideMeetingsWithoutAttendeesEnabled' to " + updatedIsEnabled);
            }
        }
    }

    public class DayTimeSpentTable : BaseVisualization, IVisualization
    {
        private readonly DateTimeOffset _date;
        private const int numberOfItemsShown = 10;
        private readonly bool _showEmailsEnabled;
        private readonly bool _showProgramsEnabled;
        private readonly bool _hideMeetingsWithoutAttendees;
        private bool? _meetingTableExists;

        public DayTimeSpentTable(DateTimeOffset date, bool showEmailsEnabled, bool showProgramsEnabled, bool hideMeetingsWithoutAttendees)
        {
            this._date = date;
            this._showEmailsEnabled = showEmailsEnabled;
            this._showProgramsEnabled = showProgramsEnabled;
            this._hideMeetingsWithoutAttendees = hideMeetingsWithoutAttendees;

            Title = "Details: Time Spent"; // (on websites, in meetings, in programs, in files, in Visual Studio projects and on code reviews)";
            IsEnabled = true; //todo: handle by user
            Order = 19; //todo: handle by user
            Size = VisSize.Wide;
            Type = VisType.Day;
        }

        public override string GetHtml()
        {
            var html = string.Empty;
            var list = new List<TimeSpentItem>();

            /////////////////////
            // fetch & combine data sets
            /////////////////////
            var websites = CollectData.GetCleanedWebsites(_date);
            list.AddRange(websites);

            var files = CollectData.GetCleanedFilesWorkedOn(_date);
            list.AddRange(files);

            var vsProjects = CollectData.GetCleanedVisualStudioProjects(_date);
            list.AddRange(vsProjects);

            var reviews = CollectData.GetCleanedCodeReviewsDone(_date);
            list.AddRange(reviews);

            // check if table exists before runnign query
            if (! _meetingTableExists.HasValue)
            {
                _meetingTableExists = Database.GetInstance().HasTable(Settings.MeetingsTable);
            }
            if (_meetingTableExists.Value)
            {
                var meetings = CollectData.GetCleanedMeetings(_date, _hideMeetingsWithoutAttendees);
                list.AddRange(meetings);
            }
            // users can disable including email data
            if (_showEmailsEnabled)
            {
                var emails = CollectData.GetCleanedOutlookInfo(_date);
                list.AddRange(emails);
            }
            // users can disable showing program details
            if (_showProgramsEnabled)
            {
                var programs = CollectData.GetCleanedPrograms(_date);
                list.AddRange(programs);
            }


            /////////////////////
            // sort the list again
            /////////////////////
            var sortedList = list.OrderByDescending(i => i.DurationInMins).ToList();


            /////////////////////
            // visualize data sets
            ////////////////////
            if (sortedList == null || sortedList.Count == 0)
            {
                html += VisHelper.NotEnoughData(string.Format(CultureInfo.InvariantCulture, "We couldn't collect any files, programs used, meetings, emails, websites, code reviews or VS projects for the {0}.", _date.Date.ToShortDateString()));
                return html;
            }

            // create blank table
            html += string.Format(CultureInfo.InvariantCulture, "<table id='{0}'>", VisHelper.CreateChartHtmlTitle(Title));
            html += "<thead><tr><th>Type</th><th>Title</th><th>Time spent</th></tr></thead>";
            html += "<tbody>";
            foreach (var i in sortedList)
            {
                html += "<tr>";
                html += "<td>" + i.Type + "</td>";
                html += "<td>" + GetFormattedTitle(i.Title) + "</td>";
                html += "<td>" + GetFormattedDuration(i.DurationInMins) + "</td>";
                html += "</tr>";
            }
            html += "</tbody>";
            html += "</table>";

            /////////////////////
            // create & add javascript
            ////////////////////
            var js =  "<script type='text/javascript'>"
                    + "var tf = new TableFilter('" + VisHelper.CreateChartHtmlTitle(Title) + "', { base_path: '/', "
                    + "col_0: 'select', col_2: 'none', popup_filters: false, auto_filter: true, auto_filter_delay: 700, highlight_keywords: true, " // filtering options  (column 0: checklist or select)
                    + "alternate_rows: true, " // styling options
                    + "col_widths:[ '5.625em', '40em', '5.625em'], " // fixed columns sizes
                    + "grid_layout: true, grid_width: '51.25em', grid_height: '16.4em', grid_cont_css_class: 'grd-main-cont', grid_tblHead_cont_css_class: 'grd-head-cont', " // styling & behavior of the table
                                                                                                                                                                                 //+ "extensions: [{name: 'sort', types: [ 'string', 'string', 'null'] }], "
                    + "msg_filter: 'Filtering...', display_all_text: 'Show all', no_results_message: true, watermark: ['', 'Type to filter...', ''], "
                    + "}); " // no content options
                    + "tf.init(); "
                    + "</script>";

            html += " " + js;

            return html;
        }

        private string GetFormattedDuration(double duration)
        {
            var formatted = "";
            if (duration >= 60) formatted = Math.Round((duration / 60.0), 1) + " hrs";
            else if (duration >= 10) formatted = Math.Round(duration, 0) + " mins";
            else formatted = Math.Round(duration, 1) + " mins";
            return formatted; 
        }

        private string GetFormattedTitle(string title)
        {
            var maxLength = 100;
            if (title.Length > maxLength)
            {
                title = title.Substring(0, maxLength-3) + "...";
            }
            return title;
        }
    }
}
