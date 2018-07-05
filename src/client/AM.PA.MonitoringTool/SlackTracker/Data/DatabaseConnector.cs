
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
        private const string CHANNEL_NAME = "channel_name";
        private const string LAST_UPDATED = "id";
        private const string BEING_USED = "id";
        private const string SENDER = "id";
        private const string RECEIVER = "id";
        private const string TIMESTAMP = "id";
        private const string MESSAGE = "id";

        private static readonly string CREATE_CHANNELS_TABLE_QUERY = "CREATE TABLE IF NOT EXISTS " + Settings.CHANNELS_TABLE_NAME + " ("
                                                                     + CHANNEL_NAME + " TEXT PRIMARY KEY, "
                                                                     + LAST_UPDATED + " TEXT, "
                                                                     + BEING_USED + " INTEGER)";

        private static readonly string CREATE_LOGS_TABLE_QUERY = "CREATE TABLE IF NOT EXISTS " + Settings.LOG_TABLE_NAME + " ("
                                                                 + SENDER + " TEXT PRIMARY KEY, "
                                                                 + RECEIVER + " TEXT, "
                                                                 + TIMESTAMP + " TEXT UNIQUE, "
                                                                 + MESSAGE + " TEXT)";

        #region create
        internal static void CreateSlackTables()
        {
            try
            {
                Database.GetInstance().ExecuteDefaultQuery(CREATE_CHANNELS_TABLE_QUERY);
                Database.GetInstance().ExecuteDefaultQuery(CREATE_LOGS_TABLE_QUERY);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }
        #endregion
    }
}
