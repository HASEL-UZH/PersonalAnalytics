using Shared.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowRecommender.Data
{
    internal static class Queries
    {
        internal static void CreateTables()
        {
            var db = Database.GetInstance();
            db.ExecuteDefaultQuery($@"CREATE TABLE IF NOT EXISTS {Settings.EventTable} (id INTEGER PRIMARY KEY, time TEXT, event Text, windowId TEXT, processName TEXT, windowTitle TEXT, zIndex INTEGER, rank INTEGER, score REAL);");
        }

        internal static void DropTables()
        {
            var db = Database.GetInstance();
            db.ExecuteDefaultQuery($@"DROP TABLE IF EXISTS {Settings.EventTable};");
        }

        internal static void SaveEvent(EventName eventName, DatabaseEntry entry)
        {
            var db = Database.GetInstance();
            var query = $@"INSERT INTO {Settings.EventTable} (time, event, windowId, processName, windowTitle, zIndex, rank, score) VALUES (?, ?, ?, ?, ?, ?, ?, ?);";
            var parameters = new object[] { DateTime.Now, eventName.ToString(), entry.WindowHandle, entry.ProcessName, entry.WindowTitle, entry.ZIndex, entry.Rank, entry.Score };
            db.ExecuteDefaultQuery(query, parameters);
        }

        internal static void SaveEvents(EventName eventName, IEnumerable<DatabaseEntry> entries)
        {
            var db = Database.GetInstance();
            var query = $@"INSERT INTO {Settings.EventTable} (time, event, windowId, processName, windowTitle, zIndex, rank, score) VALUES (?, ?, ?, ?, ?, ?, ?, ?);";
            var parameterList = entries
                .Select(entry => new object[] { DateTime.Now, eventName.ToString(), entry.WindowHandle, entry.ProcessName, entry.WindowTitle, entry.ZIndex, entry.Rank, entry.Score });
            db.ExecuteBatchQueries(query, parameterList);
        }

        internal static bool GetEnabledSetting()
        {
            var db = Database.GetInstance();
            return db.GetSettingsBool(Settings.EnabledSettingDatabaseKey, Settings.EnabledDefault);
        }

        internal static void SetEnabledSetting(bool enabled)
        {
            var db = Database.GetInstance();
            db.SetSettings(Settings.EnabledSettingDatabaseKey, enabled);
            db.LogInfo($"The participant updated the setting '{Settings.EnabledSettingDatabaseKey}' to {enabled}.");
        }

        internal static bool GetTreatmentModeSetting()
        {
            var db = Database.GetInstance();
            return db.GetSettingsBool(Settings.TreatmentModeSettingDatabaseKey, Settings.TreatmentModeDefault);
        }

        internal static void SetTreatmentModeSetting(bool treatmentMode)
        {
            var db = Database.GetInstance();
            db.SetSettings(Settings.TreatmentModeSettingDatabaseKey, treatmentMode);
            db.LogInfo($"The participant updated the setting '{Settings.TreatmentModeSettingDatabaseKey}' to {treatmentMode}.");
        }

        internal static int GetNumberOfWindowsSetting()
        {
            var db = Database.GetInstance();
            return db.GetSettingsInt(Settings.NumberOfWindowsSettingDatabaseKey, Settings.NumberOfWindowsDefault);
        }

        internal static void SetNumberOfWindowsSetting(int numberOfWindows)
        {
            var db = Database.GetInstance();
            db.SetSettings(Settings.NumberOfWindowsSettingDatabaseKey, numberOfWindows.ToString());
            db.LogInfo($"The participant updated the setting '{Settings.NumberOfWindowsSettingDatabaseKey}' to {numberOfWindows}.");
        }
    }

    internal sealed class EventName
    {
        private readonly string _name;

        public static readonly EventName Initial = new EventName("Initial");
        public static readonly EventName Open = new EventName("Open");
        public static readonly EventName Focus = new EventName("Focus");
        public static readonly EventName Close = new EventName("Close");
        public static readonly EventName Minimize = new EventName("Minimize");

        private EventName(string name)
        {
            _name = name;
        }

        public override string ToString()
        {
            return _name;
        }
    }

    internal struct DatabaseEntry
    {
        internal readonly string WindowHandle;
        internal readonly string WindowTitle;
        internal readonly string ProcessName;
        internal readonly int ZIndex;
        internal readonly int Rank;
        internal readonly double Score;

        internal DatabaseEntry(IntPtr windowHandle, string windowTitle, string processName, int zIndex)
        {
            WindowHandle = windowHandle.ToString();
            WindowTitle = windowTitle;
            ProcessName = processName;
            ZIndex = zIndex;
            Rank = -1;
            Score = -1;
        }

        internal DatabaseEntry(IntPtr windowHandle, string windowTitle, string processName, int zIndex, int rank, double score) : this(windowHandle, windowTitle, processName, zIndex)
        {
            Rank = rank;
            Score = score;
        }
    }
}
