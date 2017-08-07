// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-15
// 
// Licensed under the MIT License.

using System;

namespace TaskDetectionTracker
{
    public class Settings
    {
        public const string TrackerName = "Task Detection Tracker";

        public const string DbTable_TaskDetection_Sessions = "task_detection_sessions";
        public const string DbTable_TaskDetection_Validations = "task_detection_validations";

#if DEBUG 
        public static bool IsEnabledByDefault = true;
        public static TimeSpan PopUpInterval = TimeSpan.FromSeconds(10);
        public static TimeSpan PopUpReminderInterval = TimeSpan.FromSeconds(30);
        public static TimeSpan MaximumValidationInterval = TimeSpan.FromHours(3);
#elif Pilot_TaskDetection_March17
        public static bool IsEnabledByDefault = true;
                public static TimeSpan PopUpInterval = TimeSpan.FromMinutes(60); // show validation pop-up once every 60 minutes
        public static TimeSpan PopUpReminderInterval = TimeSpan.FromMinutes(5);
        public static TimeSpan MaximumValidationInterval = TimeSpan.FromHours(3);
#else
        public static bool IsEnabledByDefault = false; // disabled for all non-study users
        public static TimeSpan PopUpInterval = TimeSpan.FromMinutes(60); // show validation pop-up once every 60 minutes
        public static TimeSpan PopUpReminderInterval = TimeSpan.FromMinutes(5);
        public static TimeSpan MaximumValidationInterval = TimeSpan.FromHours(3);
#endif

        public const int MinimumProcessTimeInSeconds = 10; // TODO: change??? 60; // delete processes smaller than this threshold
        public const int MinimumProcessWidth = 50;
        public const int MinimumTimeLineWidth = 2000;
    }
}