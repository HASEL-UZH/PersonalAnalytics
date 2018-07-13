// Created by Rohit Kaushik (f20150115@goa.bits-pilani.ac.in) at the University of Zurich
// Created: 2018-07-10
// 
// Licensed under the MIT License.

using Shared;
using Shared.Data;
using System;
using System.Data;
using System.Collections.Generic;
using SlackTracker.Data.SlackModel;

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
        private const string ID = "id";
        private const string CHANNEL = "channel";
        private const string SENDER = "sender";
        private const string RECEIVER = "receiver";
        private const string TIMESTAMP = "timestamp";
        private const string MESSAGE = "message";

        //User Fields
        private const string USER_ID = "user_id";
        private const string TEAM_ID = "team_id";
        private const string NAME = "name";
        private const string REAL_NAME = "real_name";
        private const string IS_BOT = "is_bot";


        //Create Queries
        private static readonly string CREATE_CHANNELS_TABLE_QUERY = "CREATE TABLE IF NOT EXISTS " + Settings.CHANNELS_TABLE_NAME + " ("
                                                                     + CHANNEL_ID + " TEXT PRIMARY KEY, "
                                                                     + CHANNEL_NAME + " TEXT, "
                                                                     + CREATED + " INTEGER, "
                                                                     + CREATOR + " TEXT)";

        private static readonly string CREATE_LOGS_TABLE_QUERY = "CREATE TABLE IF NOT EXISTS " + Settings.LOG_TABLE_NAME + " ("
                                                                 + ID + " INTEGER PRIMARY KEY, "
                                                                 + TIMESTAMP + " TEXT, "
                                                                 + CHANNEL + " TEXT, "
                                                                 + SENDER + " TEXT, "
                                                                 + RECEIVER + " TEXT, "
                                                                 + MESSAGE + " TEXT)";

        private static readonly string CREATE_USERS_TABLE_QUERY = "CREATE TABLE IF NOT EXISTS " + Settings.USER_TABLE_NAME + " ("
                                                                  + USER_ID + " TEXT PRIMARY KEY, "
                                                                  + TEAM_ID + " TEXT, "
                                                                  + NAME + " TEXT, "
                                                                  + REAL_NAME + " TEXT, "
                                                                  + IS_BOT + " BIT NOT NULL)";


        //Insert Queries
        private static readonly string INSERT_CHANNEL_QUERY = "INSERT OR IGNORE INTO " + Settings.CHANNELS_TABLE_NAME
                                                              + " (" + CHANNEL_ID + ", "
                                                              + CHANNEL_NAME + ", "
                                                              + CREATED + ", "
                                                              + CREATOR + ") VALUES ({0}, {1}, {2}, {3})";

        private static readonly string INSERT_USER_QUERY = "INSERT OR IGNORE INTO " + Settings.USER_TABLE_NAME
                                                           + " (" + USER_ID + ", "
                                                           + TEAM_ID + ", "
                                                           + NAME + ", "
                                                           + REAL_NAME + ", "
                                                           + IS_BOT + ") VALUES ({0}, {1}, {2}, {3}, {4})";

        private static readonly string INSERT_LOG_QUERY = "INSERT OR IGNORE INTO " + Settings.LOG_TABLE_NAME
                                                          + " (" + TIMESTAMP + ", " 
                                                          + CHANNEL + ", "
                                                          + SENDER + ", "
                                                          + RECEIVER + ", "
                                                          + MESSAGE + ") VALUES ({0}, {1}, {2}, {3}, {4})";



        #region create
        internal static void CreateSlackTables()
        {
            try
            {
                Database.GetInstance().ExecuteDefaultQuery(CREATE_CHANNELS_TABLE_QUERY);
                Database.GetInstance().ExecuteDefaultQuery(CREATE_LOGS_TABLE_QUERY);
                Database.GetInstance().ExecuteDefaultQuery(CREATE_USERS_TABLE_QUERY);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }
        #endregion

        #region insert
        public static void SaveUsers (IList<User> users)
        {
            foreach(User user in users)
            {
                string query = String.Empty;
                query += String.Format(INSERT_USER_QUERY, "'" + user.id + "'", "'" + user.team_id + "'", "'" + user.name + "'", "'" + user.real_name + "'", "'" + user.is_bot + "'");
                Logger.WriteToConsole("query: " + query);
                Database.GetInstance().ExecuteDefaultQuery(query);
            }
        }

        public static void SaveChannels (IList<Channel> channels)
        {
            foreach(Channel channel in channels)
            {
                String query = String.Empty;
                query += String.Format(INSERT_CHANNEL_QUERY, "'" + channel.id + "'", "'" + channel.name + "'", "'" + channel.created + "'", "'" + channel.creator + "'");
                Logger.WriteToConsole("query: " + query);
                Database.GetInstance().ExecuteDefaultQuery(query);
            }
        }

        public static void SaveLogs (IList<Log> logs)
        {
            foreach(Log log in logs)
            {
                String query = String.Empty;
                query += String.Format(INSERT_LOG_QUERY,  "'" + log.timestamp.ToString(Settings.FORMAT_DAY_AND_TIME) + "'", "'" + log.channel_id + "'", "'" + log.sender + "'", "'" + "null" + "'", "'" + log.message.Replace("'", "''") + "'");
                Logger.WriteToConsole("query: " + query);
                Database.GetInstance().ExecuteDefaultQuery(query);
            }
        }
        #endregion

        #region select

        public static string GetLastTimeSynced()
        {
            return Database.GetInstance().GetSettingsString(Settings.LAST_SYNCED_DATE, "never");
        }
        
        public static void GetUserWithID()
        {

        }

        public static List<Channel> GetChannels()
        {
            var result = new List<Channel>();

            try
            {
                string tableName = Settings.CHANNELS_TABLE_NAME;
                string query = "SELECT * FROM " + tableName;

                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    Logger.WriteToConsole(row[2].ToString());
                    result.Add(new Channel
                    {
                        id = row[0].ToString(),
                        name = row[1].ToString(),
                        created = Int64.Parse(row[2].ToString()),
                        creator = row[3].ToString()
                    });
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return result;
        }

        /// <summary>
        /// Return Log in sequential order for the day
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static List<Log> GetLogForDate (DateTime date, Channel channel)
        {
            var result = new List<Log>();

            try
            {
                string tableName = Settings.LOG_TABLE_NAME;
                string query = "SELECT * FROM " + tableName + " WHERE TIMESTAMP = " + "'" + date.ToString(Settings.FORMAT_DAY) + "'" + " ORDER BY " + ID;

                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    result.Add(new Log
                    {
                        id = (int)row[0],
                        channel_id = row[1].ToString(),
                        sender = row[2].ToString(),
                        receiver = row[3].ToString(),
                        timestamp = DateTime.Parse(row[4].ToString()),
                        message = row[5].ToString()
                    });
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return result;
        }

        public static void GetLogForWeek (DateTime date, Channel channel)
        {

        }

        #endregion
    }
}
