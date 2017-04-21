// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using Shared.Data;
using Shared.Helpers;
using System;
using System.IO;
using System.Windows.Media;

namespace Shared
{
    public static class Settings
    {
        /**
         * version 1 - initial release
         * version 2 - update emails table in MsOfficeTracker (2016-06-20)
         * version 3 - update focus_state table in FlowTracker (2017-01-12)
         */
        public const int DatabaseVersion = 3; // !!! update when existing database table changes (have a look at PerformDatabaseUpdatesIfNecessary() for details)


#if Dev
        public const bool IsUploadEnabled = false;
        public const bool IsUploadReminderEnabled = false;
        public static bool IsFeedbackEnabled = true; // can be overwritten when starting the retrospection
        private const int _checkForToolUpdatesIntervalInMins = 5;
#elif Pilot_MSR
        public const bool IsUploadEnabled = true;
        public const bool IsUploadReminderEnabled = true;
        public static bool IsFeedbackEnabled = false; // can be overwritten when starting the retrospection
        private const int _checkForToolUpdatesIntervalInMins = 6 * 60;
#elif Pilot_TaskDetection_March17
        public const bool IsUploadEnabled = false;
        public const bool IsUploadReminderEnabled = false;
        public static bool IsFeedbackEnabled = false; // can be overwritten when starting the retrospection
        private const int _checkForToolUpdatesIntervalInMins = 6 * 60;
#else
        public const bool IsUploadEnabled = false;
        public const bool IsUploadReminderEnabled = false;
        public static bool IsFeedbackEnabled = true; // can be overwritten when starting the retrospection
        private const int _checkForToolUpdatesIntervalInMins = 6 * 60;
#endif

        public static bool AnonymizeSensitiveData = false;
        public const bool PrintQueriesToConsole = false;

        internal const string LogDbTable = "log";
        public const string SettingsDbTable = "settings";
        public const string FeedbackDbTable = "feedback";
        public const string TimeZoneTable = "timezone";

        public const string WindowsActivityTable = "windows_activity"; //used for the retrospection
        //public const string UserEfficiencySurveyTable = "user_efficiency_survey"; // used for the retrospection
        //public const string EmailsTable = "emails"; // used for the retrospection
        public const string MeetingsTable = "meetings";  // used for the retrospection

        public static TimeSpan CheckForStudyDataSharedReminderInterval = TimeSpan.FromHours(4); // every 4 hours, check if we should remind the user to share study data
        public static TimeSpan CheckForToolUpdatesInterval = TimeSpan.FromMinutes(_checkForToolUpdatesIntervalInMins); // every x minutes, check if there is an update available
        public static TimeSpan TooltipIconUpdateInterval = TimeSpan.FromSeconds(20); // every 20 seconds, update the tasktray icon tool tip
        public static TimeSpan RemindToResumeToolInterval = TimeSpan.FromMinutes(30); // every 30 minutes, check if the tool is still paused, if yes: remind the user

        public static string ExportFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PersonalAnalytics");
        //public static string RemoteFolderName = "remote\\";

        public const string RegAppName = "PersonalAnalytics"; // set manually
        public const int Port = 57827; // needed for the retrospection (local web server)

        // path (Regedit): Computer\HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
        public const string RegAppPath = @"\PersonalAnalytics\PersonalAnalytics\PersonalAnalytics.appref-ms"; // change also publisher name in .csproj


        ////////////////////////////////////////////////////////////
        // retrospection constants
        // hint: any changes must also be done in styles_css
        //internal static double ItemSmallWidthEm = (11.875 - sideMargin);
        //internal static double ItemSmallHeightEm = (10 - titleMargin);
        //internal static double ItemSquareWidthEm = (25 - sideMargin);
        //internal static double ItemSquareHeightEm = (21.25 - titleMargin);
        //internal static double ItemWideWidthEm = (51.25 - sideMargin);
        //internal static double ItemWideHeightEm = (21.25 - titleMargin);
        //internal const double sideMargin = 1.25; //2.5; //1.25;
        //internal const double titleMargin = 4.0; //5.625; //3.125;


        ////////////////////////////////////////////////////////////
        // contact emails
#if PilotMSR
        public const string EmailAddress1 = "tzimmer@microsoft.com"; // main email address
        public const string EmailAddress2 = "ameyer@ifi.uzh.ch";
#else
        public const string EmailAddress1 = "ameyer@ifi.uzh.ch"; // main email address
        public static string EmailAddress2 = string.Empty;
#endif


        ////////////////////////////////////////////////////////////
        // Colors
        public const string RetrospectionColorHex = "#007acc";
        private static SolidColorBrush _retrospectionColor = (SolidColorBrush)(new BrushConverter().ConvertFrom(RetrospectionColorHex));
        public static SolidColorBrush RetrospectionColorBrush { get { return _retrospectionColor; } }

        public const string GrayColor = "#E8E8E8";
        private static SolidColorBrush _grayColor = (SolidColorBrush)(new BrushConverter().ConvertFrom(GrayColor));
        public static SolidColorBrush GrayColorBrush { get { return _grayColor; } }

        public const string DarkGrayColor = "#808080";
        private static SolidColorBrush _darkGrayColor = (SolidColorBrush)(new BrushConverter().ConvertFrom(DarkGrayColor));
        public static SolidColorBrush DarkGrayColorBrush { get { return _darkGrayColor; } }

    }

}