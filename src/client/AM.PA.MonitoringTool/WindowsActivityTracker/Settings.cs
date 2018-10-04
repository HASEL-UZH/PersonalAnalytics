// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using System.Collections.Generic;

namespace WindowsActivityTracker
{
    public static class Settings
    {
        public const bool IsEnabled = true; // should never be disabled by the user (too many services rely on it)
        public static bool RecordIdle = true;
        internal const int NotCountingAsIdleInterval_ms = 2 * 60 * 1000; //in ms
        internal const int IdleTimerInterval_ms = 1000; // in ms
        internal const string DbTable = "windows_activity";

        internal const int IdleSleepValidate_TimerInterval_ms = 20 * 60 * 1000; // in ms
        internal const int IdleSleepValidate_ThresholdIdleBlocks_s = 10 * 60; // block sized that are considered for validation (2min)
        internal const int IdleSleepValidate_ThresholdBack_short_h = 2; // if checked in near past -> go back 2 hours
        internal const int IdleSleepValidate_ThresholdBack_long_d = 70; // if not checked in the near past -> go back 70 days (not more: perf!)
        internal const string ManualSleepIdle = "ManualSleep";
        internal const string IdleSleepLastValidated = "IdleSleepLastValidated";

        internal static List<string> InkognitoBrowsingTerms = new List<string> { "inprivate", "private browsing", "incognito" }; // works in Edge, Firefox, NOT: Chrome
    }
}
