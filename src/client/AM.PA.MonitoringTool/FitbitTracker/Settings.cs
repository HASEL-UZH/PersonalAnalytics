// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-23
// 
// Licensed under the MIT License.

namespace FitbitTracker
{
    internal class Settings
    {
        //Deamon
        internal const string TRACKER_NAME = "Fitbit Tracker";
        internal const string TRACKER_ENEABLED_SETTING = "FitbitTrackerEnabled";
        internal const int SYNCHRONIZE_INTERVALL = 20 * 60 * 1000;

        //Database table names
        internal static readonly string SLEEP_TABLE_NAME = "fitbit_sleep";
        internal static readonly string SLEEP_SUMMARY_TABLE_NAME = "fitbit_sleep_summary";
        internal static readonly string DOWNLOAD_TABLE_NAME = "fitbit_downloads";

        //Database field names
        internal static readonly string DOWNLOAD_START_DATE = "FitbitDownloadStartDate";
        internal static readonly string ACCESS_TOKEN = "FitbitAccessToken";
        internal static readonly string REFRESH_TOKEN = "FitbitRefreshToken";

        //OTHER
        internal static readonly string FORMAT_DAY = "dd-MM-yyyy";
        internal static readonly string FORMAT_DAY_AND_TIME = "HH:mm:ss dd-MM-yyyy";
    }

}