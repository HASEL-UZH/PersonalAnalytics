// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using System;
using System.IO;

namespace Shared
{
    public static class Settings
    {
        public const bool PrintQueriesToConsole = false;

        public const string LogDbTable = "log";
        public const string SettingsDbTable = "settings";
        public const string WindowsActivityTable = "windows_activity"; //used for visualizations
        public static string UserEfficiencySurveyTable = "user_efficiency_survey"; // used for visualizations
        public static string UserInputKeyboardTable = "user_input_keyboard"; // used for visualizations
        public static string UserInputMouseClickTable = "user_input_mouse_click"; // used for visualiations
        public static string UserInputMouseMovementTable = "user_input_mouse_movement"; // used for visualiations
        public static string UserInputMouseScrollingTable = "user_input_mouse_scrolling"; // used for visualiations

        public const int WindowsActivityCheckerInterval = 10000; //TODO: remove // (used to be 60000); shouldn't be changed; defined here as its used for the visualizations too
        public const int MiniSurveyIntervalDefaultValue = 60; //every 2h
        public const int UserInputVisMinutesInterval = 10;

        public static TimeSpan TrackerManagerUpdateInterval = TimeSpan.FromSeconds(10);
        public static string ExportFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PersonalAnalytics");
        public static string RemoteFolderName = "remote\\";

        public const string RegAppName = "PersonalDeveloperAnalytics"; // set manually
        public const int Port = 57827; // needed for the visualizations
        public static string SettingsTitle = "Settings";

        // path (Regedit): Computer\HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
        public const string RegAppPath = @"\Andre Meyer (S.E.A.L., University of Zurich)\Personal Analytics\Personal Analytics.appref-ms"; //TODO: maybe change

        ////////////////////////////////////////////////////////////
        // user input level weighting
        // Keystroke Ratio = 1 (base unit)
        public const int MouseClickKeyboardRatio = 3;
        public const double MouseMovementKeyboardRatio = 0.0028;
        public const double MouseScrollingKeyboardRatio = 1.55;
        //public const double MouseScrollingKeyboardRatio = 0.016;
    }
}
