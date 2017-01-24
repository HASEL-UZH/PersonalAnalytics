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

        //Database
        internal static readonly string TABLE_NAME = "fitbit";
        internal static readonly string ACCESS_TOKEN = "FitbitAccessToken";
        internal static readonly string REFRESH_TOKEN = "FitbitRefreshToken";
    }

}