// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

namespace WindowsActivityTracker
{
    public static class Settings
    {
        public const bool IsEnabled = true;
        public static bool RecordWindowTitles = true;
        public static bool RecordIdle = true; // implementing now
        public const int NotCountingAsIdleInterval = 120000; //in ms
        public const int IdleTimerIntervalInMilliseconds = 1000; // in ms //TODO: maybe uses too much resources?
        public const string DbTable = "windows_activity";
    }
}
