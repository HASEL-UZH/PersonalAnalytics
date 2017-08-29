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

        public const string NumberOfValidationsCompleted_Setting = "TaskDetectionNumValidations";

#if DEBUG 
        public static bool IsEnabledByDefault = true;
        internal static int NumberOfPopUpsWithoutPredictions = 2;
        internal static TimeSpan PopUpInterval = TimeSpan.FromSeconds(300);
        internal static TimeSpan PopUpReminderInterval_Short = TimeSpan.FromSeconds(30);
        internal static TimeSpan PopUpReminderInterval_Long = TimeSpan.FromSeconds(60);
        internal static TimeSpan MaximumValidationInterval = TimeSpan.FromHours(1);
#elif Pilot_TaskDetection_March17
        public static bool IsEnabledByDefault = true;
        internal static int NumberOfPopUpsWithoutPredictions = 5;
        internal static TimeSpan PopUpInterval = TimeSpan.FromMinutes(60); // show validation pop-up once every 60 minutes
        internal static TimeSpan PopUpReminderInterval_Short = TimeSpan.FromMinutes(5);
        internal static TimeSpan PopUpReminderInterval_Long = TimeSpan.FromMinutes(15);
        internal static TimeSpan MaximumValidationInterval = TimeSpan.FromHours(1);
#else
        internal static int NumberOfPopUpsWithoutPredictions = 2;
        public static bool IsEnabledByDefault = false; // disabled for all non-study users
        internal static TimeSpan PopUpInterval = TimeSpan.FromMinutes(60); // show validation pop-up once every 60 minutes
        internal static TimeSpan PopUpReminderInterval_Short = TimeSpan.FromMinutes(5);
        internal static TimeSpan PopUpReminderInterval_Long = TimeSpan.FromMinutes(15);
        internal static TimeSpan MaximumValidationInterval = TimeSpan.FromHours(1);
#endif

        internal const int MaximumTimePostponed_Minutes = 20;
        internal const int MinimumProcessTime_Seconds = 10; // delete processes smaller than this threshold
        internal const int MinimumProcessWidth = 50; 
        internal const int MaximumTimeLineWidth = 2500; 
        internal const int MinimumTaskDuration_Seconds = 60;
    }
}