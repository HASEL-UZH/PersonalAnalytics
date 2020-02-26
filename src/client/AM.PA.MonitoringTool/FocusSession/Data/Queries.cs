// Created by Philip Hofmann (philip.hofmann@uzh.ch) from the University of Zurich
// Created: 2020-02-11
// 
// Licensed under the MIT License.

namespace FocusSession.Data
{
    public class Queries
    {
        internal static void CreateFocusTable()
        {
            try
            {
                Shared.Data.Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.FocusTimerTable + " (id INTEGER PRIMARY KEY, startTime TEXT, endTime TEXT, timeDuration TEXT);");

            }
            catch (System.Exception e)
            {
                Shared.Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Updated Database tables if the version changed
        /// </summary>
        /// <param name="version"></param>
        internal static void UpdateDatabaseTables(int version)
        {
            try
            {
            }
            catch (System.Exception e)
            {
                Shared.Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Saves the timestamp into the database
        /// </summary>
        /// <param name="date"> Provide the start and endDate</param>

        internal static void SaveTime(System.DateTime startTime, System.DateTime stopTime, System.TimeSpan timeDuration)
        {
            try
            {
                Shared.Data.Database.GetInstance().ExecuteDefaultQuery("INSERT INTO " + Settings.FocusTimerTable + " (startTime, endTime, timeDuration) VALUES (" + Shared.Data.Database.GetInstance().QTime(startTime) + ", " + Shared.Data.Database.GetInstance().QTime(stopTime) + ", " + Shared.Data.Database.GetInstance().QTime(timeDuration) + ");");
            }
            catch (System.Exception e)
            {
                Shared.Logger.WriteToLogFile(e);
            }
        }

    }
}
