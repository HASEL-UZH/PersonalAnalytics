// Created by Rohit Kaushik (f20150115@goa.bits-pilani.ac.in) at the University of Zurich
// Created: 2018-07-09
// 
// Licensed under the MIT License.

using Shared;
using Shared.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackTracker.Data
{
    class DatabaseConnector
    {
        //Channel Fields
        private const string CHANNEL_ID = "channel_id";
        private const string CHANNEL_NAME = "channel_name";
        private const string CREATED = "channel_created";
        private const string CREATOR = "created_by";
        //Log Fields
        private const string SENDER = "id";
        private const string RECEIVER = "id";
        private const string TIMESTAMP = "id";
        private const string MESSAGE = "id";

        //Create Queries
        private static readonly string CREATE_CHANNELS_TABLE_QUERY = "CREATE TABLE IF NOT EXISTS " + Settings.CHANNELS_TABLE_NAME + " ("
                                                                     + CHANNEL_ID + " TEXT PRIMARY KEY, "
                                                                     + CHANNEL_NAME + " TEXT, "
                                                                     + CREATED + " INTEGER, "
                                                                     + CREATOR + " TEXT)";

        private static readonly string CREATE_LOGS_TABLE_QUERY = "CREATE TABLE IF NOT EXISTS " + Settings.LOG_TABLE_NAME + " ("
                                                                 + SENDER + " TEXT PRIMARY KEY, "
                                                                 + RECEIVER + " TEXT, "
                                                                 + TIMESTAMP + " TEXT UNIQUE, "
                                                                 + MESSAGE + " TEXT)";

        //Insert Queries
        private static readonly string INSERT_CHANNEL_QUERY = "INSERT OR IGNORE INTO " + Settings.CHANNELS_TABLE_NAME
                                                              + "(" + CHANNEL_ID + ", "
                                                              + CHANNEL_NAME + ", "
                                                              + CREATED + ", "
                                                              + CREATOR + ") VALUES ({0}, {1}, {2}, {3})";

        #region create
        internal static void CreateSlackTables()
        {
            try
            {
                Database.GetInstance().ExecuteDefaultQuery(CREATE_CHANNELS_TABLE_QUERY);
                //Database.GetInstance().ExecuteDefaultQuery(CREATE_LOGS_TABLE_QUERY);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }
        #endregion

        public static string GetLastTimeSynced()
        {
            return Database.GetInstance().GetSettingsString(Settings.LAST_SYNCED_DATE, "never");
        }
    }
}
