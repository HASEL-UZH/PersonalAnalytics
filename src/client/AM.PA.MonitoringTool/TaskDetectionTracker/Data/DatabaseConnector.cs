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
using TaskDetectionTracker.Helpers;

namespace TaskDetectionTracker.Data
{
    internal class DatabaseConnector
    {
        private static string QUERY_CREATE_SESSION = "CREATE TABLE IF NOT EXISTS " + Settings.DbTable_TaskDetection_Sessions + " (sessionId INTEGER PRIMARY KEY, time DATETIME, session_start DATETIME, session_end DATETIME, timePopUpResponded DATETIME, comments TEXT);";
        private static string QUERY_CREATE_VALIDATION = "CREATE TABLE IF NOT EXISTS " + Settings.DbTable_TaskDetection_Validations + " (id INTEGER PRIMARY KEY, sessionId INTEGER, time DATETIME, task_start DATETIME, task_end DATETIME, task_detection_case TEXT, task_type_proposed TEXT, task_type_validated TEXT);";

        private static string QUERY_INSERT_SESSION = "INSERT INTO " + Settings.DbTable_TaskDetection_Sessions + " (time, session_start, session_end, timePopUpResponded, comments) VALUES ({0}, {1}, {2}, {3}, {4});";
        private static string QUERY_INSERT_VALIDATION = "INSERT INTO " + Settings.DbTable_TaskDetection_Validations + " (sessionId, time, task_start, task_end, task_detection_case, task_type_proposed, task_type_validated) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6});";

        internal static void CreateTaskDetectionValidationTable()
        {
            Database.GetInstance().ExecuteDefaultQuery(QUERY_CREATE_SESSION);
            Database.GetInstance().ExecuteDefaultQuery(QUERY_CREATE_VALIDATION);
        }

        /// <summary>
        /// Saves the session information to DbTable_TaskDetection_Sessions (returns a sessionId)
        /// </summary>
        /// <returns></returns>
        internal static int TaskDetectionSession_SaveToDatabase(DateTime sessionStart, DateTime sessionEnd, DateTime timePopUpResponded, string comment)
        {
            try
            {
                var query = string.Format(QUERY_INSERT_SESSION,
                                          "strftime('%Y-%m-%d %H:%M:%f', 'now', 'localtime')",
                                          Database.GetInstance().QTime(sessionStart), //TODO: QTime2
                                          Database.GetInstance().QTime(sessionEnd), //TODO: QTime2
                                          Database.GetInstance().QTime(timePopUpResponded), //TODO: QTime2
                                          Database.GetInstance().Q(comment));
                Database.GetInstance().ExecuteDefaultQuery(query);

                var query2 = "SELECT last_insert_rowid();";
                return Database.GetInstance().ExecuteScalar(query2);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return 0;
            }
        }

        /// <summary>
        /// Saves the validated task detections for this session (with sessionId)
        /// </summary>
        /// <param name="taskDetections"></param>
        internal static void TaskDetectionValidationsPerSession_SaveToDatabase(int sessionId, List<TaskDetection> taskDetections)
        {
            var db = Database.GetInstance();

            try
            {
                foreach (var task in taskDetections)
                {
                    var query = string.Format(QUERY_INSERT_VALIDATION,
                                          sessionId, 
                                          "strftime('%Y-%m-%d %H:%M:%f', 'now', 'localtime')",
                                          db.QTime(task.Start), //TODO: QTime2
                                          db.QTime(task.End), //TODO: QTime2
                                          db.Q(task.TaskDetectionCase.ToString()),
                                          db.Q(task.TaskTypeProposed),
                                          db.Q(task.TaskTypeValidated));
                                          // doesn't store isMaintask yet
                    db.ExecuteDefaultQuery(query);
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
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
                string query = "SELECT time, window, process FROM windows_activity WHERE " + "(" + " STRFTIME('%s', DATETIME(time)) between STRFTIME('%s', DATETIME('" + from.ToString("u") + "')) and STRFTIME('%s', DATETIME('" + to.ToString("u") + "')) "+ " ) ";
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

        /// <summary>
        /// Returns the number of keystrokes per minute between the start and the end date
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        internal static List<KeystrokeData> GetKeystrokeData(DateTime start, DateTime end)
        {
            var result = new List<KeystrokeData>();

            try
            {
                string query = "SELECT tsStart, tsEnd, keyTotal FROM user_input WHERE " + "(" + " STRFTIME('%s', DATE(tsStart)) between STRFTIME('%s', DATE('" + start.ToString("u") + "')) and STRFTIME('%s', DATE('" + end.ToString("u") + "')) " + " ) AND " + "(" + " STRFTIME('%s', DATE(tsEnd)) between STRFTIME('%s', DATE('" + start.ToString("u") + "')) and STRFTIME('%s', DATE('" + end.ToString("u") + "')) " + " );";
                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    DateTime startTime = DateTime.Parse(row[0].ToString());
                    DateTime endTime = DateTime.Parse(row[1].ToString());
                    int keys = Int32.Parse(row[2].ToString());
                    result.Add(new KeystrokeData { Start = startTime, End = endTime, Keystrokes = keys });
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return result;
        }

        /// <summary>
        /// Returns the number of mouse clicks per minute between the start and the end date
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        internal static List<MouseClickData> GetMouseClickData(DateTime start, DateTime end)
        {
            var result = new List<MouseClickData>();

            try
            {
                string query = "SELECT tsStart, tsEnd, clickTotal FROM user_input WHERE " + "(" + " STRFTIME('%s', DATE(tsStart)) between STRFTIME('%s', DATE('" + start.ToString("u") + "')) and STRFTIME('%s', DATE('" + end.ToString("u") + "')) " + " ) AND " + "(" + " STRFTIME('%s', DATE(tsEnd)) between STRFTIME('%s', DATE('" + start.ToString("u") + "')) and STRFTIME('%s', DATE('" + end.ToString("u") + "')) " + " );";
                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    DateTime startTime = DateTime.Parse(row[0].ToString());
                    DateTime endTime = DateTime.Parse(row[1].ToString());
                    int clicks = Int32.Parse(row[2].ToString());
                    result.Add(new MouseClickData { Start = startTime, End = endTime, Mouseclicks = clicks });
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
