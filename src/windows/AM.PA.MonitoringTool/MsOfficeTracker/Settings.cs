// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

namespace MsOfficeTracker
{
    internal class Settings
    {
        public const string TrackerName = "Office 365 Tracker";
        public const int NoValueDefault = -1;

#if Pilot_TaskDetection_March17
        public static bool IsEnabledByDefault = false;
#else
        public static bool IsEnabledByDefault = true; // user will see a pop-up (the first start) to decide whether to use it or not
#endif

        public const int SaveEmailCountsInterval_InMinutes = 20; // in minutes
        public const int UpdateCacheForDays = 10;

#if DEBUG
        public const int WaitTimeUntilTimerFirstTicks_InSeconds = 20; // in seconds
#else
        public const int WaitTimeUntilTimerFirstTicks_InSeconds = 120; // in seconds
#endif

        public const string EmailsTable = "emails";
        public const string MeetingsTable = "meetings";


        ////////////////////////////////////////////////////////////
        // constants for using the Office 365 API
        internal const string LoginApiEndpoint = "https://login.microsoftonline.com/{0}"; // default: "common"
        //internal const string ClientId = ""; // register app here to get the ClientId: https://apps.dev.microsoft.com/#/appList
        internal static readonly string[] Scopes = { "mail.read", "calendars.read" };           
        internal const string GraphApiEndpoint = "https://outlook.office.com/api/v2.0";
    }
}
