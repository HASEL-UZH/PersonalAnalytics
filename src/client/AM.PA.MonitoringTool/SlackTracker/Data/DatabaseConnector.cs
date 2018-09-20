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
using System.Web.UI.WebControls;

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
        private const string START = "start_time";
        private const string END = "end";

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

        private static readonly string CREATE_USER_ACTIVITY_TABLE = "CREATE TABLE IF NOT EXISTS " + Settings.USER_ACTIVITY_TABLE_NAME + " ("
                                                                    + ID + " INTEGER PRIMARY KEY, "
                                                                    + FROM + " TEXT, "
                                                                    + TO + " TEXT, "
                                                                    + ABOUT + " TEXT, "
                                                                    + START + " DATETIME, "
                                                                    + END + " DATETIME)";

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

        private static readonly string INSERT_USER_ACTIVITY_QUERY =
            "INSERT OR IGNORE INTO " + Settings.USER_ACTIVITY_TABLE_NAME
                                     + " (" + FROM + ", "
                                     + TO + ", "
                                     + ABOUT + ", "
                                     + START + ", "
                                     + END + ") VALUES ({0}, {1}, {2}, {3}, {4})";


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
                Database.GetInstance().ExecuteDefaultQuery(CREATE_USER_ACTIVITY_TABLE);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }
        #endregion

        internal static void SetAnalyzedDay(DateTime day)
        {
            string query = String.Empty;
            query += String.Format(INSERT_ANALYZED_DAY_QUERY, "'" + DateTime.Now.ToString(Settings.FORMAT_DAY_AND_TIME) + "'", "'" + day.ToString(Settings.FORMAT_DAY) + "'");
            Database.GetInstance().ExecuteDefaultQuery(query);
        }

        #region insert
        public static void SaveUsers (IList<User> users)
        {
            foreach(User user in users)
            {
                string query = String.Empty;
                query += String.Format(INSERT_USER_QUERY, "'" + user.id + "'", "'" + user.team_id + "'", "'" + user.name.Replace("'", "''") + "'", "'" + user.real_name.Replace("'", "''") + "'", "'" + user.is_bot + "'");
                Database.GetInstance().ExecuteDefaultQuery(query);
            }
        }

        public static void SaveChannels (IList<Channel> channels)
        {
            foreach(Channel channel in channels)
            {
                String query = String.Empty;
                query += String.Format(INSERT_CHANNEL_QUERY, "'" + channel.id + "'", "'" + channel.name + "'", "'" + channel.created + "'", "'" + channel.creator + "'");
                Database.GetInstance().ExecuteDefaultQuery(query);
            }
        }

        public static void SaveLogs (List<LogData> logs)
        {
            foreach(LogData log in logs)
            {
                List<string> mentions = log.mentions;

                String query = String.Empty;
                query += String.Format(INSERT_LOG_QUERY,  "'" + log.timestamp.ToString(Settings.FORMAT_DAY_AND_TIME) + "'", "'" + log.channel_id + "'", "'" + log.author + "'","'" + log.message.Replace("'", "''") + "'");
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

        public static void SaveThreads(List<Thread> threads)
        {
            foreach (Thread thread in threads)
            {
              
            }
        }

        public static void SaveUserActivity(List<UserActivity> activities)
        {
            foreach (UserActivity activity in activities)
            {
                String query = String.Empty;
                query += String.Format(INSERT_USER_ACTIVITY_QUERY, "'" + activity.from + "'", "'" + activity.to + "'", "'" + string.Join(",", activity.words).Replace("'", "") + "'", "'" + activity.start_time.ToString(Settings.FORMAT_DAY_AND_TIME) + "'", "'" + activity.end_time.ToString(Settings.FORMAT_DAY_AND_TIME) + "'");

                Database.GetInstance().ExecuteDefaultQuery(query);
            }
        }
        #endregion

        #region select

        public static string GetLastTimeSynced()
        {
            return Database.GetInstance().GetSettingsString(Settings.LAST_SYNCED_DATE, "never");
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
                        id = Int32.Parse(row[0].ToString()),
                        timestamp = DateTime.Parse(row[1].ToString()),
                        channel_id = row[2].ToString(),
                        author = row[3].ToString(),
                        mentions = getUserMention(Int32.Parse(row[0].ToString())),
                        message = row[4].ToString()
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
                        id = Int32.Parse(row[0].ToString()),
                        timestamp = DateTime.Parse(row[1].ToString()),
                        channel_id = row[2].ToString(),
                        author = row[3].ToString(),
                        mentions = getUserMention(Int32.Parse(row[0].ToString())),
                        message = row[4].ToString()
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
                        id = Int32.Parse(row[0].ToString()),
                        timestamp = DateTime.Parse(row[1].ToString()),
                        channel_id = row[2].ToString(),
                        author = row[3].ToString(),
                        mentions = getUserMention(Int32.Parse(row[0].ToString())),
                        message = row[4].ToString()
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

        public static List<UserActivity> GetUserActivitiesForDay(DateTime date)
        {
            List<UserActivity> user_activity = new List<UserActivity>();
            string query = string.Empty;
            query += "SELECT * FROM " + Settings.USER_ACTIVITY_TABLE_NAME + " WHERE DATE(" + START + ") >= " +
                    "'" + date.ToString(Settings.FORMAT_DAY) + "'" + " AND DATE(" + END + ") <= " + "'" + date.ToString(Settings.FORMAT_DAY) + "'";

            var table = Database.GetInstance().ExecuteReadQuery(query);

            foreach (DataRow row in table.Rows)
            {
                user_activity.Add(new UserActivity
                {
                    channel_id = row[0].ToString(),
                    from = row[1].ToString(),
                    to = row[2].ToString(),
                    words = row[3].ToString().Split(',').ToList(),
                    start_time = DateTime.Parse(row[4].ToString()),
                    end_time = DateTime.Parse(row[5].ToString())
                });
            }

            return user_activity;
        }

        public static List<string> GetKeywordsForDate(DateTime date)
        {
            List<Channel> _channels = GetChannels();
            List<LogData> _logs = GetLog(date, _channels[0].id);

            if (_logs.Count == 0) {return new List<string>();}

            List<string> messages = _logs.Select(o => o.message).ToList();

            List<string> keywords = TextRank.getKeywords(string.Join(" ", messages));

            return keywords;
        }
        #endregion

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
    }
}
