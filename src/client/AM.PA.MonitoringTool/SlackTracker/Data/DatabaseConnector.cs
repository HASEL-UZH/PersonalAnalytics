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
using SlackTracker.Analysis;
using System.Globalization;
using System.ServiceModel.Channels;
using System.Web.UI.WebControls;
using System.Diagnostics;

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
        private const string CLUSTER = "cluster_id";

        //User Fields
        private const string USER_ID = "user_id";
        private const string TEAM_ID = "team_id";
        private const string NAME = "name";
        private const string REAL_NAME = "real_name";
        private const string IS_BOT = "is_bot";

        //Conversation Characteristics
        private const string CLUSTER_ID = "cluster_id";
        private const string TOPICS = "topics";
        private const string USER_PARTICIPATION = "user_participated";
        private const string CLUSTER_TYPE = "type of conversation";
        private const string START_TIME = "start time";
        private const string END_TIME = "end time";

        //User Activity
        private const string ACTIVITY_ID = "id";
        private const string ACTIVITY_CHANNEL = "channel_id";
        private const string FROM = "from_user_id";
        private const string TO = "to_user_id";
        private const string ABOUT = "topics_discussed";
        private const string DATE = "date";
        private const string DURATION = "duration";
        private const string INTENSITY = "intensity";

        //Keywords
        private const string KEY_ID = "keyword_id";
        private const string KEYWORD = "keyword";

        #region Junction tables
        //User Mention Fields
        private const string MENTION_ID = "mention_id";
        private const string LOG_ID = "log_mentioned_in";
        private const string USER_MENTION_ID = "user_mentioned";

        //User Activity Topics
        private const string id = "id";
        private const string keyword_id = "keyword_id";
        private const string activity_id = "activity_id";

        private const string SAVE_TIME = "save_time";
        private const string DAY = "day";
        private const string DATA = "data";
        #endregion


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
                                                                 + MESSAGE + " TEXT, "
                                                                 + CLUSTER + " INTEGER DEFAULT 0)";

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

        private static readonly string CREATE_THREADS_TABLE = "CREATE TABLE IF NOT EXISTS " + Settings.THREADS_TABLE_NAME + " ("
                                                              + CLUSTER_ID + " INTEGER PRIMARY KEY, "
                                                              + TOPICS + " INTEGER, "
                                                              + USER_PARTICIPATION + " INTEGER, "
                                                              + CLUSTER_TYPE + " TEXT, "
                                                              + START_TIME + "DATETIME, "
                                                              + END_TIME + "DATETIME)";

        private static readonly string CREATE_USER_INTERACTION_TABLE = "CREATE TABLE IF NOT EXISTS " + Settings.USER_INTERACTION_TABLE_NAME + " ("
                                                                       + ID + " INTEGER PRIMARY KEY, "
                                                                       + CHANNEL_ID + " TEXT, "
                                                                       + FROM + " TEXT, "
                                                                       + TO + " TEXT, "
                                                                       + ABOUT + " TEXT, "
                                                                       + DATE + " DATETIME, "
                                                                       + DURATION + " INTEGER)";

        public static readonly string CREATE_USER_ACTIVITY_TABLE = "CREATE TABLE IF NOT EXISTS " + Settings.USER_ACTIVITY_TABLE_NAME + " ("
                                                                + ID + " INTEGER PRIMARY KEY, "
                                                                + FROM + " TEXT, "
                                                                + TO + " TEXT, "
                                                                + TIMESTAMP + " DATETIME, "
                                                                + INTENSITY + " INTEGER)";

        private static readonly string CREATE_KEYWORDS_TABLE = "CREATE TABLE IF NOT EXISTS " + Settings.KEYWORDS_TABLE_NAME + " ("
                                                               + KEY_ID + " INTEGER PRIMARY KEY, "
                                                               + KEYWORD + " INTEGER)";

        private static readonly string CREATE_ANALYSIS_TABLE = "CREATE TABLE IF NOT EXISTS " + Settings.ANALYSIS_TABLE_NAME + " ("
                                                               + ID + " INTEGER PRIMARY KEY, "
                                                               + SAVE_TIME + " TEXT, "
                                                               + DAY + " TEXT)";

        //Update Queries
        private static readonly string UPDATE_LOG_CLUSTER = "UPDATE " + Settings.LOG_TABLE_NAME
                                                            + " SET " + CLUSTER + " = {0}, "
                                                            + "WHERE " + LOG_ID + " = {1};";
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
                                                          + MESSAGE + ") VALUES ({0}, {1}, {2}, {3})";

        private static readonly string INSERT_THREAD_QUERY = "INSERT OR IGNORE INTO " + Settings.THREADS_TABLE_NAME
                                                             + " (" + TOPICS + ", "
                                                             + USER_PARTICIPATION + ", "
                                                             + CLUSTER_TYPE + ", "
                                                             + START_TIME + ", "
                                                             + END_TIME +
                                                             ") VALUES ({0}, {1}, {2}, {3}, {4})";

        private static readonly string INSERT_ANALYZED_DAY_QUERY = "INSERT INTO " + Settings.ANALYSIS_TABLE_NAME
                                                                   + "(" + SAVE_TIME + ", "
                                                                   + DAY + ") VALUES ({0}, {1})";

        private static readonly string INSERT_USER_INTERACTION_QUERY = "INSERT OR IGNORE INTO " + Settings.USER_INTERACTION_TABLE_NAME
                                                                        + " (" + CHANNEL_ID + ", "
                                                                        + FROM + ", "
                                                                        + TO + ", "
                                                                        + ABOUT + ", "
                                                                        + DATE + ", "
                                                                        + DURATION + ") VALUES ({0}, {1}, {2}, {3}, {4}, {5})";

        private static readonly string INSERT_USER_ACTIVITY_QUERY = "INSERT OR IGNORE INTO " + Settings.USER_ACTIVITY_TABLE_NAME
                                                                    + " (" + FROM + ", "
                                                                    + TO + ", "
                                                                    + TIMESTAMP + ", "
                                                                    + INTENSITY + ") VALUES ({0}, {1}, {2}, {3})";


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
                Database.GetInstance().ExecuteDefaultQuery(CREATE_KEYWORDS_TABLE);
                Database.GetInstance().ExecuteDefaultQuery(CREATE_THREADS_TABLE);
                Database.GetInstance().ExecuteDefaultQuery(CREATE_ANALYSIS_TABLE);
                Database.GetInstance().ExecuteDefaultQuery(CREATE_USER_INTERACTION_TABLE);
                Database.GetInstance().ExecuteDefaultQuery(CREATE_USER_ACTIVITY_TABLE);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }
        #endregion

        #region insert

        internal static void SetAnalyzedDay(DateTime day)
        {
            string query = String.Empty;
            query += String.Format(INSERT_ANALYZED_DAY_QUERY, "'" + DateTime.Now.ToString(Settings.FORMAT_DAY_AND_TIME) + "'", "'" + day.ToString(Settings.FORMAT_DAY) + "'");
            Database.GetInstance().ExecuteDefaultQuery(query);
        }

        public static void SaveUsers (IList<User> users)
        {
            foreach(User user in users)
            {
                string query = String.Empty;
                query += String.Format(INSERT_USER_QUERY, "'" + user.Id + "'", "'" + user.TeamId + "'", "'" + user.Name.Replace("'", "''") + "'", "'" + user.RealName.Replace("'", "''") + "'", "'" + user.IsBot + "'");
                Database.GetInstance().ExecuteDefaultQuery(query);
            }
        }

        public static void SaveChannels (IList<Channel> channels)
        {
            foreach(Channel channel in channels)
            {
                String query = String.Empty;
                query += String.Format(INSERT_CHANNEL_QUERY, "'" + channel.Id + "'", "'" + channel.Name + "'", "'" + channel.Created + "'", "'" + channel.Creator + "'");
                Database.GetInstance().ExecuteDefaultQuery(query);
            }
        }

        public static void SaveLogs (List<LogData> logs)
        {
            foreach(LogData log in logs)
            {
                List<string> mentions = log.Mentions;

                String query = String.Empty;
                query += String.Format(INSERT_LOG_QUERY,  "'" + log.Timestamp.ToString(Settings.FORMAT_DAY_AND_TIME) + "'", "'" + log.ChannelId + "'", "'" + log.Author + "'","'" + log.Message.Replace("'", "''") + "'");
                Database.GetInstance().ExecuteDefaultQuery(query);

                //Update user mentions tables
                foreach (string mention in mentions)
                {
                    String query2 = String.Empty;
                    query2 += String.Format(INSERT_USER_MENTION_QUERY, "'" + log.Id + "'", "'" + mention + "'");
                    Database.GetInstance().ExecuteDefaultQuery(query2);
                }
            }
        }

        public static void SaveThreads(List<Thread> threads)
        {
            foreach (Thread thread in threads)
            {
              
            }
        }

        public static void SaveUserInteraction(List<UserInteraction> activities)
        {
            foreach (UserInteraction activity in activities)
            {
                String query = String.Empty;
                query += String.Format(INSERT_USER_INTERACTION_QUERY, "'" + activity.ChannelName + "'", "'" + activity.From + "'", "'" + activity.To + "'", activity.Topics.Count == 0 ? "null" : "'" + string.Join(",", activity.Topics).Replace("'", "") + "'", "'" + activity.Date.ToString(Settings.FORMAT_DAY) + "'", "'" + activity.Duration + "'");

                Database.GetInstance().ExecuteDefaultQuery(query);
            }
        }

        public static void SaveUserActivity(List<UserActivity> activities)
        {
            foreach (UserActivity activity in activities)
            {
                String query = String.Empty;
                query += String.Format(INSERT_USER_ACTIVITY_QUERY, "'" + activity.From + "'", "'" + activity.To + "'", "'" + activity.Time.ToString(Settings.FORMAT_DAY_AND_TIME) + "'", "'" + activity.Intensity + "'");

                Database.GetInstance().ExecuteDefaultQuery(query);
            }
        }
        #endregion

        #region select

        public static string GetLastTimeSynced()
        {
            return Database.GetInstance().GetSettingsString(Settings.LAST_SYNCED_DATE, "never");
        }

        internal static List<DateTime> GetDaysToAnalyze()
        {
            string query1 = string.Empty;
            string query2 = string.Empty;
            List<DateTime> daysInDatabase = new List<DateTime>();
            List<DateTime> daysAnalysed = new List<DateTime>();

            query1 += "SELECT DISTINCT DATE(" + TIMESTAMP + ") FROM " + Settings.LOG_TABLE_NAME + " ORDER BY " + TIMESTAMP;
            var table = Database.GetInstance().ExecuteReadQuery(query1);

            foreach (DataRow row in table.Rows)
            {
                daysInDatabase.Add(DateTime.ParseExact(row[0].ToString(), Settings.FORMAT_DAY, CultureInfo.InvariantCulture));
            }

            query2 += "SELECT " + DAY + " FROM " + Settings.ANALYSIS_TABLE_NAME;
            var table2 = Database.GetInstance().ExecuteReadQuery(query2);

            foreach (DataRow row in table2.Rows)
            {
                daysAnalysed.Add(DateTime.ParseExact(row[0].ToString(), Settings.FORMAT_DAY, CultureInfo.InvariantCulture));
            }

            return daysInDatabase.Where(f => !daysAnalysed.Any(t => t.Day == f.Day && t.Year == f.Year && t.Month == f.Month)).ToList();
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
                    result.Add(new Channel
                    {
                        Id = row[0].ToString(),
                        Name = row[1].ToString(),
                        Created = Int64.Parse(row[2].ToString()),
                        Creator = row[3].ToString()
                    });
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return result;
        }

        private static List<string> GetUserMention(int log_id)
        {
            var result = new List<string>();

            try
            {
                string tableName = Settings.USER_MENTION_TABLE_NAME;

                string query = "SELECT " + USER_MENTION_ID +  " FROM " + tableName + " WHERE " + LOG_ID + " = " + "'" + log_id + "'";

                var table = Database.GetInstance().ExecuteReadQuery(query);

                if(table == null) { return result; }

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

        public static List<LogData> GetLog(DateTime date)
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
                        Id = Int32.Parse(row[0].ToString()),
                        Timestamp = DateTime.Parse(row[1].ToString()),
                        ChannelId = row[2].ToString(),
                        Author = row[3].ToString(),
                        Mentions = GetUserMention(Int32.Parse(row[0].ToString())),
                        Message = row[4].ToString()
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
        public static List<LogData> GetLog(DateTime date, string channel_id)
        {
            var result = new List<LogData>();

            try
            {
                string tableName = Settings.LOG_TABLE_NAME;
                string query = "SELECT * FROM " + tableName + " WHERE DATE(" + TIMESTAMP + ") = " + "'" + date.ToString(Settings.FORMAT_DAY) + "'" + " AND " + CHANNEL + "= '" + channel_id + "'" + " ORDER BY " + ID;

                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    result.Add(new LogData
                    {
                        Id = Int32.Parse(row[0].ToString()),
                        Timestamp = DateTime.Parse(row[1].ToString()),
                        ChannelId = row[2].ToString(),
                        Author = row[3].ToString(),
                        Mentions = GetUserMention(Int32.Parse(row[0].ToString())),
                        Message = row[4].ToString()
                    });
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return result;
        }

        public static List<LogData> GetLog(DateTime startdate, DateTime enddate)
        {
            var result = new List<LogData>();

            try
            {
                string tableName = Settings.LOG_TABLE_NAME;
                string query = "SELECT * FROM " + tableName + " WHERE DATE(" + TIMESTAMP + ") >= "
                               + "'" + startdate.ToString(Settings.FORMAT_DAY) + "'"
                               + " AND DATE(" + TIMESTAMP + ") <= " + "'" + enddate.ToString(Settings.FORMAT_DAY) + "'"
                               + " ORDER BY " + ID;

                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    result.Add(new LogData
                    {
                        Id = Int32.Parse(row[0].ToString()),
                        Timestamp = DateTime.Parse(row[1].ToString()),
                        ChannelId = row[2].ToString(),
                        Author = row[3].ToString(),
                        Mentions = GetUserMention(Int32.Parse(row[0].ToString())),
                        Message = row[4].ToString()
                    });
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return result;
        }

        public static List<UserInteraction> GetUserInteractionsForDay(DateTime date)
        {
            List<UserInteraction> user_activity = new List<UserInteraction>();
            string query = string.Empty;
            query += "SELECT * FROM " + Settings.USER_INTERACTION_TABLE_NAME;
 
            var table = Database.GetInstance().ExecuteReadQuery(query);

            foreach (DataRow row in table.Rows)
            {
                user_activity.Add(new UserInteraction
                {
                    ChannelName = row[1].ToString(),
                    From = row[2].ToString(),
                    To = row[3].ToString(),
                    Topics = new HashSet<string>(row[4].ToString().Split(',').ToList()),
                    Date = DateTime.Parse(row[5].ToString()),
                    Duration = Int32.Parse(row[6].ToString())
                });
            }

            return user_activity;
        }

        public static List<UserActivity> GetUserActivitiesForDay(DateTime date)
        {
            List<UserActivity> user_activity = new List<UserActivity>();
            string query = string.Empty;
            query += "SELECT * FROM " + Settings.USER_ACTIVITY_TABLE_NAME + " WHERE DATE(" + TIMESTAMP + ") == " +
                     "'" + date.ToString(Settings.FORMAT_DAY) + "'";

            var table = Database.GetInstance().ExecuteReadQuery(query);

            foreach (DataRow row in table.Rows)
            {
                user_activity.Add(new UserActivity()
                {
                    From = row[1].ToString(),
                    To = row[2].ToString(),
                    Time = DateTime.Parse(row[3].ToString()),
                    Intensity = Int32.Parse(row[4].ToString())
                });
            }

            return user_activity;
        }

        public static List<string> GetKeywordsForDate(DateTime date)
        {
            List<Channel> _channels = GetChannels();
            List<LogData> _logs = GetLog(date, _channels[0].Id);

            if (_logs.Count == 0) {return new List<string>();}

            List<string> messages = _logs.Select(o => o.Message).ToList();

            List<string> keywords = TextRank.GetKeywords(string.Join(" ", messages));

            return keywords;
        }

        public static string GetUserNameFromId(string user_id)
        {
            string query = string.Empty;
            query += "SELECT " + NAME + " FROM " + Settings.USER_TABLE_NAME + " WHERE " + USER_ID + " == " + "'" +
                     user_id + "'";
            Logger.WriteToConsole(query);
            var table = Database.GetInstance().ExecuteReadQuery(query);

            foreach (DataRow dataRow in table.Rows)
            {
                foreach (var item in dataRow.ItemArray)
                {
                    Logger.WriteToConsole(string.Join(" ", item));
                }
            }

            return table.Rows[0][0].ToString();
        }

        public static string GetChannelNameFromId(string channel_id)
        {
            string query = string.Empty;
            query += "SELECT " + CHANNEL_NAME + " FROM " + Settings.CHANNELS_TABLE_NAME + " WHERE " + CHANNEL_ID +
                     " == " + "'" + channel_id + "'";

            var table = Database.GetInstance().ExecuteReadQuery(query);

            foreach (DataRow dataRow in table.Rows)
            {
                foreach (var item in dataRow.ItemArray)
                {
                    Logger.WriteToConsole(string.Join(" ", item));
                }
            }

            return table.Rows[0][0].ToString();
        }
        #endregion
    }
}
