// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using System;
using Shared;
using Shared.Data;
using System.Data;

namespace BiometricsTracker.Data
{
    public class DatabaseConnector
    {
        private static readonly string ID = "id";
        private static readonly string TIME = "time";
        private static readonly string HEARTRATE = "heartrate";

        private static readonly string TABLE_NAME = "biometrics";
        private static readonly string CREATE_QUERY = "CREATE TABLE IF NOT EXISTS " + TABLE_NAME + " (" + ID + " INTEGER PRIMARY KEY, " + TIME + " TEXT, " + HEARTRATE + " INTEGER)";
        private static readonly string INSERT_QUERY = "INSERT INTO " + TABLE_NAME + "(" + TIME + ", " + HEARTRATE + ") VALUES (strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'), {1})";
        
        internal static void CreateBiometricTables()
        {
            try
            {
                Database.GetInstance().ExecuteDefaultQuery(CREATE_QUERY);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        internal static void AddHeartrateToDatabase(DateTimeOffset timestamp, int heartrate)
        {
            try
            {
                string query = String.Format(INSERT_QUERY, timestamp, heartrate);
                Database.GetInstance().ExecuteDefaultQuery(query);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        internal static double GetAverageHeartrate(DateTimeOffset date, VisType type)
        {
            var query = "SELECT ROUND(AVG(" + HEARTRATE + "), 2) FROM " + TABLE_NAME + " WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(type, date, TIME);
            var table = Database.GetInstance().ExecuteReadQuery(query);

            if (table.Rows.Count == 1)
            {
                DataRow row = table.Rows[0];

                if (row.IsNull(0))
                {
                    return Double.NaN;
                }
                else
                {
                    return Convert.ToDouble(row.ItemArray.GetValue(0));
                }
            }
            else
            {
                return Double.NaN;
            }

        }
    }
}