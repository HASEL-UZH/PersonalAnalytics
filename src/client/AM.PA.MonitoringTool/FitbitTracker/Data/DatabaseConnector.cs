// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using Shared;
using Shared.Data;
using System;

namespace FitbitTracker.Data
{

    public class DatabaseConnector
    {

        private const string ID = "id";
        private const string TIME = "time";
        private const string STEPS = "steps";

        private static readonly string CREATE_QUERY = "CREATE TABLE IF NOT EXISTS " + Settings.TABLE_NAME + " (" + ID + " INTEGER PRIMARY KEY, " + TIME + " TEXT, " + STEPS + " INTEGER)";

        #region create
        internal static void CreateFitbitTables()
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
        #endregion

    }

}