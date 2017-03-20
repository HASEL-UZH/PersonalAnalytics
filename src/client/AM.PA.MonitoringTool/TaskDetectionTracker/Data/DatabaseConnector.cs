// Created by Andre Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-20
// 
// Licensed under the MIT License.

using Shared.Data;

namespace TaskDetectionTracker.Data
{
    internal class DatabaseConnector
    {
        internal static void CreateTaskDetectionValidationTable()
        {
            Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.DbTable_TaskDetection_Sessions + " (sessionId INTEGER PRIMARY KEY, time TEXT, session_start TEXT, session_end TEXT, timePopUpResponded TEXT, comments TEXT);");
            Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.DbTable_TaskDetection_Validations + " (id INTEGER PRIMARY KEY, sessionId INTEGER, time TEXT, task_start TEXT, task_end TEXT, task_detection_case TEXT, task_type_proposed TEXT, task_type_validated TEXT);");
        }
    }
}
