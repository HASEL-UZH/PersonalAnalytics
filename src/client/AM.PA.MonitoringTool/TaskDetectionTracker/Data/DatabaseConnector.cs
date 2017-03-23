// Created by Andre Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-20
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Shared.Data;
using TaskDetectionTracker.Model;
using System.Data;
using Shared;

namespace TaskDetectionTracker.Data
{
    internal class DatabaseConnector
    {
        internal static void CreateTaskDetectionValidationTable()
        {
            Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.DbTable_TaskDetection_Sessions + " (sessionId INTEGER PRIMARY KEY, time TEXT, session_start TEXT, session_end TEXT, timePopUpResponded TEXT, comments TEXT);");
            Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.DbTable_TaskDetection_Validations + " (id INTEGER PRIMARY KEY, sessionId INTEGER, time TEXT, task_start TEXT, task_end TEXT, task_detection_case TEXT, task_type_proposed TEXT, task_type_validated TEXT);");
        }

        /// <summary>
        /// Saves the user validated task detections to the database.
        /// 
        /// 1. saves the session information to DbTable_TaskDetection_Sessions (returns a sessionId)
        /// 2. saves the validated task detections for this session (with sessionId)
        /// </summary>
        /// <param name="taskDetections"></param>
        internal static void TaskDetectionSession_SaveToDatabase(List<TaskDetection> taskDetections)
        {
            // TODO: implement
            Shared.Logger.WriteToConsole("## Not yet implemented: TaskDetectionSession_SaveToDatabase!");
        }

        /// <summary>
        /// Returns all processes from the windows_activity table within a given time frame
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        internal static List<TaskDetectionInput> GetProcesses(DateTime from, DateTime to)
        {
            List<TaskDetectionInput> result = new List<TaskDetectionInput>();

            try
            {
                string query = "Select time, window, process from windows_activity where time >= '" + from + "' AND time < '" + to + "';";
                var table = Database.GetInstance().ExecuteReadQuery(query);
                foreach (DataRow row in table.Rows)
                {
                    DateTime start = DateTime.Parse(row[0].ToString());
                    string window = row[1].ToString();
                    string processName = row[2].ToString();
                    var process = new TaskDetectionInput { Start = start, WindowTitles = new List<string> { window }, ProcessName = processName };
                    result.Add(process);
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
            
            return result;
        }
    }
}
