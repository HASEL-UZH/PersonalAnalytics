// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-24
// 
// Licensed under the MIT License.

using Shared;
using Shared.Data;
using System;
using FitbitTracker.Model;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Globalization;

namespace FitbitTracker.Data
{

    public class DatabaseConnector
    {

        private const string ID = "id";
        private const string SAVE_TIME = "save_time";
        private const string AWAKE_COUNT = "awakeCount";
        private const string AWAKE_DURATION = "awakeDuration";
        private const string DATE_OF_SLEEP = "dateOfSleep";
        private const string DURATION = "duration";
        private const string IS_MAIN_SLEEP = "isMainSleep";
        private const string EFFICIENCY = "efficiency";
        private const string LOG_ID = "logID";
        private const string MINUTES_AFTER_WAKEUP = "minutesAfterWakeUp";
        private const string MINUTES_ASLEEP = "minutesAsleep";
        private const string MINUTES_AWAKE = "minutesAwake";
        private const string MINUTES_TO_FALL_ASLEEP = "minutesToFallAsleep";
        private const string RESTLESS_COUNT = "restlessCount";
        private const string RESTLESS_DURATION = "restlessDuration";
        private const string START_TIME = "startTime";
        private const string TIME_IN_BED = "timeInBed";
        private const string TOTAL_MINUTES_ASLEEP = "totalMinutesAsleep";
        private const string TOTAL_SLEEP_RECORDS = "totalSleepRecords";
        private const string TOTAL_TIME_IN_BED = "totalTimeInBed";
        private const string SLEEP_SUMMARY_ID = "sleepSummaryID";
        private const string DATA = "data";
        private const string DAY = "day";

        private static readonly string CREATE_SLEEP_TABLE_QUERY = "CREATE TABLE IF NOT EXISTS " + Settings.SLEEP_TABLE_NAME + " ("
                                                                + ID + " INTEGER PRIMARY KEY,"
                                                                + SLEEP_SUMMARY_ID + " INTEGER, "
                                                                + SAVE_TIME + " TEXT, "
                                                                + AWAKE_COUNT + " INTEGER, "
                                                                + AWAKE_DURATION + " INTEGER, "
                                                                + DATE_OF_SLEEP + " TEXT, "
                                                                + DURATION + " INTEGER, "
                                                                + EFFICIENCY + " INTEGER, "
                                                                + IS_MAIN_SLEEP + " INTEGER, "
                                                                + LOG_ID + " TEXT, "
                                                                + MINUTES_AFTER_WAKEUP + " INTEGER, "
                                                                + MINUTES_ASLEEP + " INTEGER, "
                                                                + MINUTES_AWAKE + " INTEGER, "
                                                                + MINUTES_TO_FALL_ASLEEP + " INTEGER, "
                                                                + RESTLESS_COUNT + " INTEGER, "
                                                                + RESTLESS_DURATION + " INTEGER, "
                                                                + START_TIME + " TEXT, "
                                                                + TIME_IN_BED + " INTEGER)";

        private static readonly string CREATE_SLEEP_SUMMARY_TABLE_QUERY = "CREATE TABLE IF NOT EXISTS " + Settings.SLEEP_SUMMARY_TABLE_NAME + " ("
                                                                + ID + " INTEGER PRIMARY KEY, "
                                                                + SAVE_TIME + " TEXT, "
                                                                + DATE_OF_SLEEP + " TEXT UNIQUE, "
                                                                + TOTAL_MINUTES_ASLEEP + " INTEGER, "
                                                                + TOTAL_SLEEP_RECORDS + " INTEGER, "
                                                                + TOTAL_TIME_IN_BED + " INTEGER)";

        private static readonly string CREATE_DOWNLOAD_TABLE_QUERY = "CREATE TABLE IF NOT EXISTS " + Settings.DOWNLOAD_TABLE_NAME + " ("
                                                                + ID + " INTEGER PRIMARY KEY, "
                                                                + SAVE_TIME + " TEXT, "
                                                                + DAY + " TEXT, "
                                                                + DATA + " TEXT)";

        private static readonly string INSERT_SYNCHRONIZED_DAY_QUERY = "INSERT INTO " + Settings.DOWNLOAD_TABLE_NAME
                                                                + "(" + SAVE_TIME + ", "
                                                                + DAY + ", "
                                                                + DATA + ") VALUES ({0}, {1}, {2})";

        private static readonly string INSERT_SLEEP_SUMMARY_QUERY = "INSERT INTO " + Settings.SLEEP_SUMMARY_TABLE_NAME
                                                                + "(" + SAVE_TIME + ", "
                                                                + DATE_OF_SLEEP + ", "
                                                                + TOTAL_MINUTES_ASLEEP + ", "
                                                                + TOTAL_SLEEP_RECORDS + ", "
                                                                + TOTAL_TIME_IN_BED
                                                                + ") VALUES ({0}, {1}, {2}, {3}, {4})";

        private static readonly string INSERT_OR_IGNORE_SLEEP_SUMMARY_QUERY = "INSERT OR IGNORE INTO " + Settings.SLEEP_SUMMARY_TABLE_NAME
                                                                + "(" + SAVE_TIME + ", "
                                                                + DATE_OF_SLEEP + ", "
                                                                + TOTAL_MINUTES_ASLEEP + ", "
                                                                + TOTAL_SLEEP_RECORDS + ", "
                                                                + TOTAL_TIME_IN_BED
                                                                + ") VALUES ({0}, {1}, {2}, {3}, {4})";

        private static readonly string UPDATE_SLEEP_SUMMARY_QUERY = "UPDATE " + Settings.SLEEP_SUMMARY_TABLE_NAME
                                                                + " SET " + SAVE_TIME + " = {0}, " 
                                                                + DATE_OF_SLEEP + " = {1}, "
                                                                + TOTAL_MINUTES_ASLEEP + " = {2}, "
                                                                + TOTAL_SLEEP_RECORDS + " = {3}, "
                                                                + TOTAL_TIME_IN_BED + " = {4} "
                                                                + "WHERE " + DATE_OF_SLEEP + " = {1};";

        private static readonly string INSERT_SLEEP_QUERY = "INSERT INTO " + Settings.SLEEP_TABLE_NAME
                                                                + "(" + SLEEP_SUMMARY_ID + ", "
                                                                + SAVE_TIME + ", "
                                                                + AWAKE_COUNT + ", "
                                                                + AWAKE_DURATION + ", "
                                                                + DATE_OF_SLEEP + ", "
                                                                + DURATION + ", "
                                                                + EFFICIENCY + ", "
                                                                + IS_MAIN_SLEEP + ", "
                                                                + LOG_ID + ", "
                                                                + MINUTES_AFTER_WAKEUP + ", "
                                                                + MINUTES_ASLEEP + ", "
                                                                + MINUTES_AWAKE + ", "
                                                                + MINUTES_TO_FALL_ASLEEP + ", "
                                                                + RESTLESS_COUNT + ", "
                                                                + RESTLESS_DURATION + ", "
                                                                + START_TIME + ", "
                                                                + TIME_IN_BED
                                                                + ") VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16})";

        #region create
        internal static void CreateFitbitTables()
        {
            try
            {
                Database.GetInstance().ExecuteDefaultQuery(CREATE_SLEEP_TABLE_QUERY);
                Database.GetInstance().ExecuteDefaultQuery(CREATE_SLEEP_SUMMARY_TABLE_QUERY);
                Database.GetInstance().ExecuteDefaultQuery(CREATE_DOWNLOAD_TABLE_QUERY);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }
        #endregion

        #region insert
        internal static void SaveSleepData(SleepData sleepData)
        {
            if (sleepData.Sleep.Count > 0)
            {
                SleepSummary summary = sleepData.Summary;

                string query = String.Empty;
                DateTime insert = DateTime.Now;

                query += String.Format(UPDATE_SLEEP_SUMMARY_QUERY, "'" + insert + "'", "'" + sleepData.Sleep[0].DateOfSleep.ToString(Settings.FORMAT_DAY) + "'", summary.TotalMinutesAsleep, summary.TotalSleepRecords, summary.TotalTimeInBed);
                Database.GetInstance().ExecuteDefaultQuery(query);

                query = String.Empty;
                query += String.Format(INSERT_OR_IGNORE_SLEEP_SUMMARY_QUERY, "'" + insert + "'", "'" + sleepData.Sleep[0].DateOfSleep.ToString(Settings.FORMAT_DAY) + "'", summary.TotalMinutesAsleep, summary.TotalSleepRecords, summary.TotalTimeInBed);
                Database.GetInstance().ExecuteDefaultQuery(query);

                //GET ID of previous insert
                string idquery = "SELECT * FROM " + Settings.SLEEP_SUMMARY_TABLE_NAME + " WHERE " + SAVE_TIME + " = '" + insert + "';";
                var table = Database.GetInstance().ExecuteReadQuery(idquery);

                string id = "";
                if (table.Rows.Count > 0)
                {
                    id = (table.Rows[0][0]).ToString();
                }

                foreach (SleepLog log in sleepData.Sleep)
                {
                    if (!DoesSleepLogAlreadyExists(log.LogID))
                    {
                        string sleepQuery = String.Empty;
                        sleepQuery += String.Format(INSERT_SLEEP_QUERY, id, "'" + DateTime.Now + "'", log.AwakeCount, log.AwakeDuration, "'" + log.DateOfSleep.ToString(Settings.FORMAT_DAY) + "'", log.Duration, log.Efficiency, log.IsMainSleep ? 1 : 0, log.LogID, log.MinutesAfterWakeup, log.MinutesAsleep, log.MinutesAwake, log.MinutesToFallAsleep, log.RestlessCount, log.RestlessDuration, "'" + log.StartTime.ToString(Settings.FORMAT_DAY_AND_TIME) + "'", log.TimeInBed);
                        Database.GetInstance().ExecuteDefaultQuery(sleepQuery);
                    }
                }
            }
        }

        internal static void SetSynchronizedDay(DateTimeOffset day, DataType datType)
        {
            string query = String.Empty;
            query += String.Format(INSERT_SYNCHRONIZED_DAY_QUERY, "'" + DateTime.Now.ToString(Settings.FORMAT_DAY_AND_TIME) + "'", "'" + day.ToString(Settings.FORMAT_DAY) + "'", "'" + datType + "'");
            Database.GetInstance().ExecuteDefaultQuery(query);
        }
        #endregion

        #region select
        internal static bool DoesSleepLogAlreadyExists(string sleepLogId)
        {
            string query = "SELECT * FROM " + Settings.SLEEP_TABLE_NAME + " WHERE " + LOG_ID + " = '" + sleepLogId + "'";
            var table = Database.GetInstance().ExecuteReadQuery(query);
            return table.Rows.Count > 0;
        }

        internal static List<DateTimeOffset> GetDaysToSynchronize(DataType datType)
        {
            DateTimeOffset start;
            if (Database.GetInstance().HasSetting(Settings.DOWNLOAD_START_DATE))
            {
                start = DateTime.Parse(Database.GetInstance().GetSettingsString(Settings.DOWNLOAD_START_DATE, DateTimeOffset.Now.ToString(Settings.FORMAT_DAY)));
            }
            else
            {
                start = DateTimeOffset.Now;
                Database.GetInstance().SetSettings(Settings.DOWNLOAD_START_DATE, start.ToString(Settings.FORMAT_DAY));
            }
            
            List<DateTimeOffset> daysInDatabase = new List<DateTimeOffset>();
            string query = "SELECT " + DAY + " FROM " + Settings.DOWNLOAD_TABLE_NAME + " WHERE " + DATA + " = '" + datType + "'";
            var table = Database.GetInstance().ExecuteReadQuery(query);
            
            foreach (DataRow row in table.Rows)
            {
                daysInDatabase.Add(DateTime.ParseExact(row[0].ToString(), Settings.FORMAT_DAY, CultureInfo.InvariantCulture));
            }

            List<DateTimeOffset> days = new List<DateTimeOffset>();
            while (start < DateTimeOffset.Now)
            {
                days.Add(start);
                start = start.AddDays(1);
            }

            return days.Where(f => !daysInDatabase.Any(t => t.Day == f.Day && t.Year == f.Year && t.Month == f.Month)).ToList();  
        }
        #endregion

    }

}