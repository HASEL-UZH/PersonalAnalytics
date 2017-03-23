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

        #if Pilot_TaskDetection_March17
        public static bool IsEnabledByDefault = true;
        #else
        public static bool IsEnabledByDefault = true; //TODO: change back
        #endif

        public static TimeSpan PopUpInterval = TimeSpan.FromSeconds(5); // TimeSpan.FromMinutes(60);
        public static TimeSpan PopUpReminderInterval = TimeSpan.FromSeconds(2); // TimeSpan.FromMinutes(5);

        public const int MinimumProcessTimeInSeconds = 10;
    }
}