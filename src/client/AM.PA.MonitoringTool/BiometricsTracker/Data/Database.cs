// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using System;
using Shared;
using Shared.Data;

namespace BiometricsTracker.Data
{
    public class DatabaseConnector
    {
        private static readonly string ID = "id";
        private static readonly string TIME = "time";
        private static readonly string HEARTRATE = "heartrate";

        private static readonly string TABLE_NAME = "biometrics";
        private static readonly string CREATE_QUERY = "CREATE TABLE IF NOT EXISTS " + TABLE_NAME + " (" + ID + " INTEGER PRIMARY KEY AUTOINCREMENT, " + TIME + " TEXT, " + HEARTRATE + " INTEGER)";
        private static readonly string INSERT_QUERY = "INSERT INTO " + TABLE_NAME + "(" + TIME + ", " + HEARTRATE + ") VALUES ('{0}', {1})";

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
                Logger.WriteToConsole(query);
                Database.GetInstance().ExecuteDefaultQuery(query);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }
    }
}