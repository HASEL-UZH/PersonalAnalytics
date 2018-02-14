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
        private static string QUERY_CREATE_SESSION = "CREATE TABLE IF NOT EXISTS " + Settings.DbTable_TaskDetection_Sessions + " (sessionId INTEGER PRIMARY KEY, time DATETIME, session_start DATETIME, session_end DATETIME, timePopUpFirstShown DATETIME, timePopUpResponded DATETIME, postponedInfo TEXT, comments TEXT, confidence_switch TEXT, confidence_type TEXT);";
        private static string QUERY_CREATE_PREDICTION = "CREATE TABLE IF NOT EXISTS " + Settings.DbTable_TaskDetection_Predictions + " (id INTEGER PRIMARY KEY, sessionId INTEGER, time DATETIME, task_start DATETIME, task_end DATETIME, task_type_predicted TEXT);";
        private static string QUERY_CREATE_VALIDATION = "CREATE TABLE IF NOT EXISTS " + Settings.DbTable_TaskDetection_Validations + " (id INTEGER PRIMARY KEY, sessionId INTEGER, time DATETIME, task_start DATETIME, task_end DATETIME, task_type_validated TEXT);";

        private static string QUERY_INSERT_SESSION = "INSERT INTO " + Settings.DbTable_TaskDetection_Sessions + " (time, session_start, session_end, timePopUpFirstShown) VALUES ({0}, {1}, {2}, {3});";
        private static string QUERY_UPDATE_SESSION = "UPDATE " + Settings.DbTable_TaskDetection_Sessions + " SET timePopUpResponded = {1}, postponedInfo = {2}, comments = {3}, confidence_switch = {4}, confidence_type = {5} WHERE sessionId = {0};";
        private static string QUERY_INSERT_PREDICTION = "INSERT INTO " + Settings.DbTable_TaskDetection_Predictions + " (sessionId, time, task_start, task_end, task_type_predicted) VALUES ({0}, {1}, {2}, {3}, {4});";
        private static string QUERY_INSERT_VALIDATION = "INSERT INTO " + Settings.DbTable_TaskDetection_Validations + " (sessionId, time, task_start, task_end, task_type_validated) VALUES ({0}, {1}, {2}, {3}, {4});";

        internal static void CreateTaskDetectionValidationTables()
        {
            Database.GetInstance().ExecuteDefaultQuery(QUERY_CREATE_SESSION);
            Database.GetInstance().ExecuteDefaultQuery(QUERY_CREATE_PREDICTION);
            Database.GetInstance().ExecuteDefaultQuery(QUERY_CREATE_VALIDATION);
        }

        /// <summary>
        /// Saves the session information to DbTable_TaskDetection_Sessions 
        /// (returns a sessionId)
        /// </summary>
        /// <returns></returns>
        internal static int TaskDetectionSession_Insert_SaveToDatabase(DateTime sessionStart, DateTime sessionEnd, DateTime timePopUpFirstShown)
        {
            try
            {
                var db = Database.GetInstance();
                var query = string.Format(QUERY_INSERT_SESSION,
                                          "strftime('%Y-%m-%d %H:%M:%f', 'now', 'localtime')",
                                          db.QTime(sessionStart),
                                          db.QTime(sessionEnd),
                                          db.QTime(timePopUpFirstShown));
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
        /// Updates the session information to DbTable_TaskDetection_Sessions 
        /// (given the sessionId)
        /// </summary>
        /// <returns></returns>
        internal static void TaskDetectionSession_Update_SaveToDatabase(int sessionId, DateTime timePopUpResponded, string postponedInfo, string comment, int confidenceSwitch, int confidenceType)
        {
            try
            {
                var db = Database.GetInstance();
                var query = string.Format(QUERY_UPDATE_SESSION,
                                          db.Q(sessionId),
                                          db.QTime(timePopUpResponded),
                                          db.Q(postponedInfo),
                                          db.Q(comment),
                                          db.Q(confidenceSwitch),
                                          db.Q(confidenceType));
                Database.GetInstance().ExecuteDefaultQuery(query);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Saves the predicted task detections for this session (with sessionId)
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="taskDetections_predicted"></param>
        internal static void TaskDetectionPredictionsPerSession_SaveToDatabase(int sessionId, List<TaskDetection> taskDetections_predicted)
        {
            var db = Database.GetInstance();

            try
            {
                foreach (var task in taskDetections_predicted.OrderBy(t => t.Start))
                {
                    var query = string.Format(QUERY_INSERT_PREDICTION,
                                          sessionId,
                                          "strftime('%Y-%m-%d %H:%M:%f', 'now', 'localtime')",
                                          db.QTime2(task.Start),
                                          db.QTime2(task.End),
                                          db.Q(task.TaskTypePredicted.ToString()));
                    db.ExecuteDefaultQuery(query);
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Saves the validated task detections for this session (with sessionId)
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="taskdetections_validated"></param>
        internal static void TaskDetectionValidationsPerSession_SaveToDatabase(int sessionId, List<TaskDetection> taskdetections_validated)
        {
            var db = Database.GetInstance();

            try
            {
                foreach (var task in taskdetections_validated.OrderBy(t => t.Start))
                {
                    var query = string.Format(QUERY_INSERT_VALIDATION,
                                          sessionId,
                                          "strftime('%Y-%m-%d %H:%M:%f', 'now', 'localtime')",
                                          db.QTime2(task.Start),
                                          db.QTime2(task.End),
                                          db.Q(task.TaskTypeValidated.ToString()));
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
            var result = new List<TaskDetectionInput>();

            try
            {
                var query = "SELECT process, window, tsStart, tsEnd FROM windows_activity "  //, (strftime('%s', tsEnd) - strftime('%s', tsStart)) as 'difference'
                             + "WHERE (" + " STRFTIME('%s', DATETIME(tsStart)) between STRFTIME('%s', DATETIME('" + from.ToString("u") + "')) and STRFTIME('%s', DATETIME('" + to.ToString("u") + "')) "+ " );";
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
