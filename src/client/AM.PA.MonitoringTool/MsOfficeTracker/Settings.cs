// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

namespace MsOfficeTracker
{
    internal class Settings
    {
        public const string TrackerName = "Office 365 Tracker";

        public static bool IsEnabledByDefault = true; // user will see a pop-up (the first start) to decide whether to use it or not
        public const int SaveEmailCountsIntervalInMinutes = 20; // in minutes
        public const int UpdateCacheForDays = 10;

        public const string EmailsTable = "emails";
        public const string MeetingsTable = "meetings";

        ////////////////////////////////////////////////////////////
        // constants for using the Office 365 API
        internal const string AadInstance = "https://login.microsoftonline.com/{0}";
        internal const string ClientId = "d45086c2-265d-4244-b501-0e8498d5d3fb"; //"6a0253aa-7f9a-44d8-8959-e7839de094f1";
        internal const string RedirectUriString = "urn:ietf:wg:oauth:2.0:oob";
    }
}
