﻿// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
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
        internal static TimeSpan PopUpInterval = TimeSpan.FromSeconds(20);
        internal static TimeSpan PopUpReminderInterval = TimeSpan.FromSeconds(30);
        internal static TimeSpan MaximumValidationInterval = TimeSpan.FromHours(1);
#elif Pilot_TaskDetection_March17
        public static bool IsEnabledByDefault = true;
        internal static TimeSpan PopUpInterval = TimeSpan.FromMinutes(60); // show validation pop-up once every 60 minutes
        internal static TimeSpan PopUpReminderInterval = TimeSpan.FromMinutes(5);
        internal static TimeSpan MaximumValidationInterval = TimeSpan.FromHours(1);
#else
        public static bool IsEnabledByDefault = false; // disabled for all non-study users
        internal static TimeSpan PopUpInterval = TimeSpan.FromMinutes(60); // show validation pop-up once every 60 minutes
        internal static TimeSpan PopUpReminderInterval = TimeSpan.FromMinutes(5);
        internal static TimeSpan MaximumValidationInterval = TimeSpan.FromHours(1);
#endif

        internal const int MinimumProcessTimeInSeconds = 10; // TODO: change??? 60; // delete processes smaller than this threshold
        internal const int MinimumProcessWidth = 20; // was 50
        internal const int MaximumTimeLineWidth = 1500; // was 2000
        internal const int MinimumTaskDurationInSeconds = 60;
    }
}