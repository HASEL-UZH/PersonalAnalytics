// Created by Philip Hofmann (philip.hofmann@uzh.ch) from the University of Zurich
// Created: 2020-02-11
// 
// Licensed under the MIT License.

namespace FocusSession
{
    public static class Settings
    {
        public const string TrackerName = "FocusSession Tracker";
        public static bool IsEnabledByDefault = true;
        public const string FocusTimerTable = "focusTimer";
        public static int ClosedSessionDuration = 25;
        internal const string REPLYMESSAGE_ENEABLED_SETTING = "ReplyMessageEnabled";
        internal const string WINDOWFLAGGING_ENEABLED_SETTING = "WindowFlaggingEnabled";
    }
}
