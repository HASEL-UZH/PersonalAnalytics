// Created by Rohit Kaushik (f20150115@goa.bits-pilani.ac.in) at the University of Zurich
// Created: 2018-07-10
// 
// Licensed under the MIT License.

using Shared;
using Shared.Data;
using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using SlackTracker.Data.SlackModel;
using SlackTracker.Analysis.TopicSummarization;

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

        //User Mention Fields
        private const string MENTION_ID = "mention_id";
        private const string LOG_ID = "log_mentioned_in";
        private const string USER_MENTION_ID = "user_mentioned";

        //Summary Fields
        private const string DATE = "summary_for";


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

        private static readonly string CREATE_USER_MENTION_TABLE_QUERY = "CREATE TABLE IF NOT EXISTS " + Settings.USER_MENTION_TABLE_NAME + " ("
                                                                         + MENTION_ID + " INTEGER PRIMARY KEY, "
                                                                         + LOG_ID + " INTEGER, "
                                                                         + USER_MENTION_ID + " TEXT)";


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

        private static readonly string INSERT_USER_MENTION_QUERY = "INSERT OR IGNORE INTO " + Settings.USER_MENTION_TABLE_NAME
                                                          + " (" + LOG_ID + ", "
                                                          + USER_MENTION_ID + ") VALUES ({0}, {1})";



        #region create
        internal static void CreateSlackTables()
        {
            try
            {
                Database.GetInstance().ExecuteDefaultQuery(CREATE_CHANNELS_TABLE_QUERY);
                Database.GetInstance().ExecuteDefaultQuery(CREATE_LOGS_TABLE_QUERY);
                Database.GetInstance().ExecuteDefaultQuery(CREATE_USERS_TABLE_QUERY);
                Database.GetInstance().ExecuteDefaultQuery(CREATE_USER_MENTION_TABLE_QUERY);
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

        public static void SaveLogs (List<LogData> logs)
        {
            foreach(LogData log in logs)
            {
                List<string> mentions = log.mentions;

                String query = String.Empty;
                query += String.Format(INSERT_LOG_QUERY,  "'" + log.timestamp.ToString(Settings.FORMAT_DAY_AND_TIME) + "'", "'" + log.channel_id + "'", "'" + log.author + "'", "'" + "null" + "'", "'" + log.message.Replace("'", "''") + "'");
                Database.GetInstance().ExecuteDefaultQuery(query);

                //Update user mentions tables
                foreach (string mention in mentions)
                {
                    String query2 = String.Empty;
                    query2 += String.Format(INSERT_USER_MENTION_QUERY, "'" + log.id + "'", "'" + mention + "'");
                    Database.GetInstance().ExecuteDefaultQuery(query2);
                }
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

        private static List<string> getUserMention(int log_id)
        {
            var result = new List<string>();

            try
            {
                string tableName = Settings.USER_MENTION_TABLE_NAME;

                string query = "SELECT " + USER_MENTION_ID +  " FROM " + tableName + " WHERE " + LOG_ID + " = " + "'" + log_id;

                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    result.Add(row[0].ToString());
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
        public static List<LogData> GetLogForDate (DateTime date, Channel channel)
        {
            var result = new List<LogData>();

            try
            {
                string tableName = Settings.LOG_TABLE_NAME;
                string query = "SELECT * FROM " + tableName + " WHERE DATE(" + TIMESTAMP + ") = " + "'" + date.ToString(Settings.FORMAT_DAY) + "'" + " ORDER BY " + ID;

                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    result.Add(new LogData
                    {
                        id = Int32.Parse(row[0].ToString()),
                        timestamp = DateTime.Parse(row[1].ToString()),
                        channel_id = row[2].ToString(),
                        author = row[3].ToString(),
                        mentions = getUserMention(Int32.Parse(row[0].ToString())),
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

        public static List<string> GetKeywordsForDate(DateTime date)
        {
            List<Channel> _channels = GetChannels();
            List<Log> _logs = GetLogForDate(date, _channels[0]);

            if (_logs.Count == 0) {return new List<string>();}

            List<string> messages = _logs.Select(o => o.message).ToList();

            List<string> keywords = TextRank.getKeywords(string.Join(" ", messages));

            return keywords;
        }
        #endregion
    }
}
