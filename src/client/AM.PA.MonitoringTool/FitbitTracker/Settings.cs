// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-23
// 
// Licensed under the MIT License.

using Shared.Data;

namespace FitbitTracker
{
    internal class Settings
    {
        //Deamon
        internal const string TRACKER_NAME = "Fitbit Tracker";
        internal const string TRACKER_ENEABLED_SETTING = "FitbitTrackerEnabled";
        internal const int SYNCHRONIZE_INTERVALL_FIRST = 2 * 60 * 1000; //2 minutes
        internal const int SYNCHRONIZE_INTERVALL_SECOND = 20 * 60 * 1000; //20 minutes

        //Data Collection Settings
        #if PilotManu_March17
            internal static readonly bool IsDetailedCollectionAvailable = true;
        #else
            internal static readonly bool IsDetailedCollectionAvailable = true;
        #endif

        //Database table names
        internal static readonly string SLEEP_INTRA_DAY_TABLE_NAME = "fitbit_sleep_intraday";
        internal static readonly string SLEEP_TABLE_NAME = "fitbit_sleep";
        internal static readonly string SLEEP_SUMMARY_TABLE_NAME = "fitbit_sleep_summary";
        internal static readonly string DOWNLOAD_TABLE_NAME = "fitbit_downloads";
        internal static readonly string HEARTRATE_DAY_TABLE_NAME = "fitbit_heartrate_summary";
        internal static readonly string HEARTRATE_INTRA_DAY_TABLE_NAME = "fitbit_heartrate_intraday";
        internal static readonly string STEPS_INTRA_DAY_TABLE_NAME = "fitbit_steps_intraday";
        internal static readonly string STEPS_INTRA_DAY_AGGREGATED_TABLE_NAME = "fitbit_steps_aggregated";
        internal static readonly string ACTIVITY_SUMMARY_TABLE_NAME = "fitbit_activity_summary";

        //Database field names
        internal static readonly string DOWNLOAD_START_DATE = "FitbitDownloadStartDate";
        internal static readonly string LAST_SYNCED_DATE = "FitbitLastSynced";

        //OTHER
        internal static readonly string FORMAT_TIME = "HH:mm";
        internal static readonly string FORMAT_DAY = "yyyy-MM-dd";
        internal static readonly string FORMAT_DAY_AND_TIME = "yyyy-MM-dd HH:mm:ss";
        internal static readonly string FITBIT_FORMAT_DAY = "yyyy-MM-dd";
        internal static readonly int TOKEN_LIFETIME = 60 * 60 * 24 * 1; //1 day
        internal static readonly string REGISTRATION_URL = "https://www.fitbit.com/oauth2/authorize?response_type=code&client_id=2283KD&redirect_uri=https%3A%2F%2Fgithub.com%2Fsealuzh%2FPersonalAnalytics&scope=activity%20heartrate%20location%20nutrition%20profile%20settings%20sleep%20social%20weight&expires_in=604800";
        internal static readonly string DB_FIRST_AUTHORIZATION_CODE = "FitbitFirstAuthorizationCode";
        internal static readonly string FIRST_AUTHORIZATION_CODE = Database.GetInstance().GetSettingsString(Settings.DB_FIRST_AUTHORIZATION_CODE, string.Empty);
        internal static readonly string DB_CLIENT_ID = "FitbitClientID";
        internal static readonly string CLIENT_ID = Database.GetInstance().GetSettingsString(Settings.DB_CLIENT_ID, string.Empty);
        internal static readonly string DB_CLIENT_SECRET = "FitbitClientSecret";
        internal static readonly string CLIENT_SECRET = Database.GetInstance().GetSettingsString(Settings.DB_CLIENT_SECRET, string.Empty);
        internal static readonly string REDIRECT_URI = "https://github.com/sealuzh/PersonalAnalytics";
    }

}