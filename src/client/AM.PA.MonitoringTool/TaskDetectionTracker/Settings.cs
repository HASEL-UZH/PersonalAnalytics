// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-15
// 
// Licensed under the MIT License.

namespace TaskDetectionTracker
{
    public class Settings
    {
        public const string TrackerName = "Task Detection Tracker";
        public const int MinimumProcessTimeInSeconds = 10;

        #if Pilot_TaskDetection_March17
            public static bool IsEnabledByDefault = true;
        #else
            public static bool IsEnabledByDefault = false;
        #endif

    }
}