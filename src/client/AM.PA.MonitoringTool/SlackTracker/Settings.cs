﻿// Created by Rohit Kaushik (f20150115@goa.bits-pilani.ac.in) at the University of Zurich
// Created: 2018-07-02
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SlackTracker
{
    internal class Settings
    {
        // Daemon
        internal const string TRACKER_NAME = "Slack Tracker";
        internal const string TRACKER_ENABLED_SETTING = "SlackTrackerEnabled";
        internal const int SYNCHRONIZE_INTERVAL = 8 * 20 * 1000; //1 minutes

        //Database Tables names
        internal static readonly string DOWNLOAD_TABLE_NAME = "SlackDownloads";
        internal const string CHANNELS_TABLE_NAME = "SlackChannels";
        internal const string LOG_TABLE_NAME = "SlackLogs";
        internal const string USER_MENTION_TABLE_NAME = "SlackMessageDirectedTo";
        internal const string USER_TABLE_NAME = "SlackUsers";
        internal const string THREADS_TABLE_NAME = "SlackThreads";
        internal const string USER_ACTIVITY_TABLE_NAME = "SlackUserActivity";
        internal const string USER_INTERACTION_TABLE_NAME = "SlackUserInteraction";
        internal const string KEYWORDS_TABLE_NAME = "SlackKeywords";
        internal const string ANALYSIS_TABLE_NAME = "SlackAnalysis";

        internal static readonly bool IsEnabledByDefault = true;
        internal static readonly bool IsDetailedCollectionEnabled = false; // default: disabled

        //Database field names
        internal static readonly string DOWNLOAD_START_DATE = "SlackDownloadStartDate";
        internal static readonly string LAST_SYNCED_DATE = "SlackLastSynced";
        internal static readonly string LAST_ANALYSED_DATE = "LastAnalysedDate";

        //OTHER
        internal static readonly string FORMAT_TIME = "HH:mm";
        internal static readonly string FORMAT_DAY = "yyyy-MM-dd";
        internal static readonly string FORMAT_DAY_AND_TIME = "yyyy-MM-dd HH:mm:ss";
        internal static readonly int TOKEN_LIFETIME = 60 * 60 * 24 * 30; //30 days
        internal static readonly string REGISTRATION_URL = "https://slack.com/oauth/authorize?client_id=12830536055.392728377956&redirect_uri=https%3A%2F%2Fgithub.com%2Fsealuzh%2FPersonalAnalytics&scope=channels:history%20channels:read%20users:read";
        internal static readonly string REDIRECT_URI = "https://github.com/sealuzh/PersonalAnalytics";
    }
}
