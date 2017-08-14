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
using System.Globalization;
using System.Linq;

namespace TaskDetectionTracker.Data
{
    internal class DatabaseConnector
    {
        private static string QUERY_CREATE_SESSION = "CREATE TABLE IF NOT EXISTS " + Settings.DbTable_TaskDetection_Sessions + " (sessionId INTEGER PRIMARY KEY, time DATETIME, session_start DATETIME, session_end DATETIME, timePopUpResponded DATETIME, comments TEXT);";
        private static string QUERY_CREATE_VALIDATION = "CREATE TABLE IF NOT EXISTS " + Settings.DbTable_TaskDetection_Validations + " (id INTEGER PRIMARY KEY, sessionId INTEGER, time DATETIME, task_start DATETIME, task_end DATETIME, task_detection_case TEXT, task_type_proposed TEXT, task_type_validated TEXT, is_main_task BOOLEAN);";

        private static string QUERY_INSERT_SESSION = "INSERT INTO " + Settings.DbTable_TaskDetection_Sessions + " (time, session_start, session_end, timePopUpResponded, comments) VALUES ({0}, {1}, {2}, {3}, {4});";
        private static string QUERY_INSERT_VALIDATION = "INSERT INTO " + Settings.DbTable_TaskDetection_Validations + " (sessionId, time, task_start, task_end, task_detection_case, task_type_proposed, task_type_validated, is_main_task) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7});";

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
                                          Database.GetInstance().QTime(sessionStart),
                                          Database.GetInstance().QTime(sessionEnd), 
                                          Database.GetInstance().QTime(timePopUpResponded),
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
                foreach (var task in taskDetections.OrderBy(t => t.Start))
                {
                    var query = string.Format(QUERY_INSERT_VALIDATION,
                                          sessionId, 
                                          "strftime('%Y-%m-%d %H:%M:%f', 'now', 'localtime')",
                                          db.QTime2(task.Start), 
                                          db.QTime2(task.End),
                                          db.Q(task.TaskDetectionCase.ToString()),
                                          db.Q(task.TaskTypeProposed.ToString()),
                                          db.Q(task.TaskTypeValidated.ToString()),
                                          db.Q(task.IsMainTask));
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
                string query = "SELECT process, window, tsStart, tsEnd FROM windows_activity "  //, (strftime('%s', tsEnd) - strftime('%s', tsStart)) as 'difference'
                             + "WHERE (" + " STRFTIME('%s', DATETIME(time)) between STRFTIME('%s', DATETIME('" + from.ToString("u") + "')) and STRFTIME('%s', DATETIME('" + to.ToString("u") + "')) "+ " );";
                var table = Database.GetInstance().ExecuteReadQuery(query);
                foreach (DataRow row in table.Rows)
                {
                    try
                    {
                        var process = (DBNull.Value != row["process"]) ? Shared.Helpers.ProcessNameHelper.GetFileDescriptionFromProcess((string)row["process"]) : string.Empty;
                        var window = (DBNull.Value != row["window"]) ? (string)row["window"] : string.Empty;
                        //var difference = Convert.ToInt32(row["difference"], CultureInfo.InvariantCulture);
                        var tsStart = (DBNull.Value != row["tsStart"]) ? DateTime.Parse((string)row["tsStart"], CultureInfo.InvariantCulture) : DateTime.MinValue;
                        var tsEnd = (DBNull.Value != row["tsEnd"]) ? DateTime.Parse((string)row["tsEnd"], CultureInfo.InvariantCulture) : DateTime.MinValue;

                        var processItem = new TaskDetectionInput { Start = tsStart, End = tsEnd, WindowTitles = new List<string> { window }, ProcessName = process };
                        if (tsStart != DateTime.MinValue && tsEnd != DateTime.MinValue) result.Add(processItem);
                    }
                    catch { } // don't do anything
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
                    int keys = int.Parse(row[2].ToString());
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
                    int clicks = int.Parse(row[2].ToString());
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
