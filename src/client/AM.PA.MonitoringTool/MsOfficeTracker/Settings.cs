// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

namespace MsOfficeTracker
{
    internal class Settings
    {
        public const string TrackerName = "Office 365 Tracker";

#if Pilot_TaskDetection_March17
        public static bool IsEnabledByDefault = false;
#else
        // TODO: re-enable after bug has been fixed
        public static bool IsEnabledByDefault = true; // user will see a pop-up (the first start) to decide whether to use it or not
#endif

        public const int SaveEmailCountsIntervalInMinutes = 20; // in minutes
        public const int UpdateCacheForDays = 10;

        public const string EmailsTable = "emails";
        public const string MeetingsTable = "meetings";

        ////////////////////////////////////////////////////////////
        // constants for using the Office 365 API
        internal const string LoginApiEndpoint = "https://login.microsoftonline.com/{0}";       // default: "common"
        internal const string ClientId = "5a6e510c-f900-491e-bfb3-8a6151a49c04";                //"d10fd94c-50f0-4560-aab8-9f44a98026f5"; //"d45086c2-265d-4244-b501-0e8498d5d3fb"; //"6a0253aa-7f9a-44d8-8959-e7839de094f1";
        internal static readonly string[] Scopes = { "mail.read", "calendars.read" };           // "https://outlook.office.com/mail.read", "https://outlook.office.com/calendars.read" }; // "https://outlook.office.com/user.readbasic.all" };
        internal const string GraphApiEndpoint = "https://outlook.office.com/api/v2.0";         //"https://graph.microsoft.com/v1.0/me"; 
    }
}
