using Shared.Data;
using System;

namespace WindowRecommender.Data
{
    internal static class Queries
    {
        internal static void CreateTables()
        {
            var db = Database.GetInstance();
            db.ExecuteDefaultQuery($@"CREATE TABLE IF NOT EXISTS {Settings.EventTable} (id INTEGER PRIMARY KEY, windowId TEXT, processName TEXT, event Text, rank INTEGER, score REAL, time TEXT);");
        }

        internal static void SaveEvent(IntPtr windowHandle, string processName, EventName eventName, int rank = -1, double score = -1)
        {
            var db = Database.GetInstance();
            var query = $@"INSERT INTO {Settings.EventTable} (windowId, processName, event, rank, score, time) VALUES (?, ?, ?, ?, ?, ?);";
            var parameters = new object[] { windowHandle.ToString(), processName, eventName.ToString(), rank, score, DateTime.Now };
            db.ExecuteDefaultQuery(query, parameters);
        }
    }

    internal sealed class EventName
    {
        private readonly string _name;

        public static readonly EventName Open = new EventName("Open");
        public static readonly EventName Focus = new EventName("Focus");
        public static readonly EventName Close = new EventName("Close");

        private EventName(string name)
        {
            _name = name;
        }

        public override string ToString()
        {
            return _name;
        }
    }
}
