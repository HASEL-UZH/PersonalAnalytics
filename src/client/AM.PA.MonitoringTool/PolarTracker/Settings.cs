// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-16
// 
// Licensed under the MIT License.

using System.Drawing;

namespace PolarTracker
{
    internal class Settings
    {
        //Deamon
        internal const string TRACKER_ENEABLED_SETTING = "PolarTrackerEnabled";
        internal const string HEARTRATE_TRACKER_ID_SETTING = "HeartrateTrackerID";
        internal const string HEARTRATE_TRACKER_LOCATION_SETTING = "HeartrateTrackerLocation";
        internal const string HEARTRATE_TRACKER_LOCATION_UNKNOWN = "unknown";
        internal const int SAVE_TO_DATABASE_INTERVAL = 30 * 1000;

        //Database
        internal static readonly string TABLE_NAME = "polar";
        internal static readonly string TABLE_NAME_AGGREGATED = "polar_aggregated";

        //Settings
        #if PilotManu_March17
            internal static readonly bool IsDetailedCollectionEnabled = true;
        #else
            internal static readonly bool IsDetailedCollectionEnabled = false; // default: disabled
        #endif
        internal static readonly bool IsEnabledByDefault = true;

        //Visualization for week
        internal static readonly int NUMBER_OF_BUCKETS = 5;
        internal static readonly Color START_COLOR = Color.Blue;
        internal static readonly Color END_COLOR = Color.Red;
        internal const double HEARTRATE_THRESHOLD = 200;
        internal const double RR_DIFFERENCE_THRESHOLD = 300;
    }
}