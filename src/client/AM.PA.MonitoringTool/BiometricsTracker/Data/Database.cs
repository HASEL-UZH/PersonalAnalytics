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
        private static readonly string TABLE_NAME = "biometrics";
        private static readonly string CREATE_QUERY = "CREATE TABLE IF NOT EXISTS " + TABLE_NAME + " (id INTEGER PRIMARY KEEY, time TEXT, heartrate INTEGER)";

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

    }
}