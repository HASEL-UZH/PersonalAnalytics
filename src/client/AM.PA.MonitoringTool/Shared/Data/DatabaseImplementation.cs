// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;

namespace Shared.Data
{
    enum LogType { Info, Warning, Error }

    /// <summary>
    /// This is the implementation of the database with all queries, commands, etc.
    /// </summary>
    public class DatabaseImplementation
    {
        private SQLiteConnection _connection; // null if not connected
        public string CurrentDatabaseDumpFile; // every week a new database file

        #region Execute Queries & Log Messages

        /// <summary>
        /// Executes a query (given as parameter).
        /// Also logs the query.
        /// </summary>
        /// <param name="query"></param>
        /// <returns>an int if it was successful or not</returns>
        public int ExecuteDefaultQuery(string query)
        {
            WriteQueryToLog(query);
            if (_connection == null || _connection.State != ConnectionState.Open) return 0;
            
            var cmd = new SQLiteCommand(query, _connection);
            try 
            {
                var ans = cmd.ExecuteNonQuery();
                return ans;
            } 
            catch (Exception e) 
            {
                Logger.WriteToLogFile(e);
                return 0;
            } 
            finally 
            {
                cmd.Dispose();    
            }
        }

        /// <summary>
        /// Executes a query (given as a parameter).
        /// Also logs the query.
        /// </summary>
        /// <param name="query"></param>
        /// <returns>the first table as a result or null if there was no result</returns>
        public DataTable ExecuteReadQuery(string query)
        {
            WriteQueryToLog(query);

            if (_connection == null || _connection.State != ConnectionState.Open) return null;
            var cmd = new SQLiteCommand(query, _connection);
            var da = new SQLiteDataAdapter(cmd);
            var ds = new DataSet();
            try
            {
                da.Fill(ds);
                return ds.Tables[0];
            }
            catch (Exception e)
            {
                return null;
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Inserts the message (given as a parameter) into the log-database-table (flag: Error).
        /// Also logs the query.
        /// </summary>
        /// <param name="message"></param>
        public void LogError(string message)
        {
            try
            {
                WriteQueryToLog(message, true);
                var query = "INSERT INTO " + Settings.LogDbTable + " (created, message, type) VALUES (strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'), " + Q(message) + ", " + Q(LogType.Error.ToString()) + ")";
                ExecuteDefaultQuery(query);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Inserts an unknown error with the name of the tracker into the log-database-table (flag: Error).
        /// Also logs the query.
        /// </summary>
        /// <param name="tracker"></param>
        public void LogErrorUnknown(string tracker)
        {
            var message = string.Format("An unknown exception occurred in the tracker '{0}'.", tracker);
            LogError(message);
        }

        /// <summary>
        /// Inserts the message (given as a parameter) into the log-database-table (flag: Info).
        /// Also logs the query.
        /// </summary>
        /// <param name="message"></param>
        public void LogInfo(string message)
        {
            try
            {
                WriteQueryToLog(message, true);
                var query = "INSERT INTO " + Settings.LogDbTable + " (created, message, type) VALUES (strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'), " + Q(message) + ", " + Q(LogType.Info.ToString()) + ")";
                ExecuteDefaultQuery(query);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Inserts the message (given as a parameter) into the log-database-table (flag: Warning).
        /// Also logs the query.
        /// </summary>
        /// <param name="message"></param>
        public void LogWarning(string message)
        {
            try
            {
                WriteQueryToLog(message, true);
                var query = "INSERT INTO " + Settings.LogDbTable + " (created, message, type) VALUES (strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'), " + Q(message) + ", " + Q(LogType.Warning.ToString()) + ")";
                ExecuteDefaultQuery(query);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Q is the James-Bond guy. Improves the string characters not to mess it up in the query string.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string Q(string str)
        {
            if (str == null) return "''";
            return "'" + str.Replace("'", "''") + "'";
        }

        /// <summary>
        /// Formats and magicifies a datetime
        /// '%Y-%m-%d %H:%M:%S'
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public string QTime(DateTime dateTime)
        {
            var dateTimeString = dateTime.ToString("yyyy-MM-dd HH:mm:ss"); // dateTime.ToShortDateString() + " " + dateTime.ToLongTimeString();
            return Q(dateTimeString);
        }

        /// <summary>
        /// Logs the query if the global setting allows it and it's not
        /// enforced my the calling method.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="force"></param>
        private static void WriteQueryToLog(string query, bool force = false)
        {
            if (Settings.PrintQueriesToConsole == false && force == false) return;
            Logger.WriteToConsole("Query: " + query);
        }

        #endregion

        #region Settings

        /// <summary>
        /// Saves a setting with a given key or value. Inserts new entry if nothing is yet stored
        /// or updates the value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void SetSettings(string key, string value)
        {
            try {
                // if key is not yet stored in settings
                if (new SQLiteCommand("SELECT value FROM " + Settings.SettingsDbTable + " WHERE key=" + Q(key), _connection).ExecuteScalar() == null)
                    new SQLiteCommand("INSERT INTO " + Settings.SettingsDbTable + " (key, value) VALUES (" + Q(key) + ", " + Q(value) + ")", _connection).ExecuteNonQuery();
                // if key is stored in settings
                else
                    new SQLiteCommand("UPDATE " + Settings.SettingsDbTable + " SET value=" + Q(value) + " WHERE key=" + Q(key), _connection).ExecuteNonQuery();
            } 
            catch (Exception e) 
            {
                Shared.Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Gets the stored setting value for a given key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private object GetSettings(string key)
        {
            try { return new SQLiteCommand("SELECT value FROM " + Settings.SettingsDbTable + " WHERE key=" + Q(key), _connection).ExecuteScalar(); }
            catch { return null; }
        }

        public bool WindowsContextTrackerEnabled
        {
            get
            {
                try
                {
                    var ret = (string)GetSettings("WindowsContextTrackerEnabled");
                    if (ret == null) return false; // default enabled
                    return (ret == "1");
                }
                catch { return true; }
            }
            set { SetSettings("WindowsContextTrackerEnabled", value ? "1" : "0"); }
        }

        public bool MiniSurveysEnabled
        {
            get
            {
                try
                {
                    var ret = (string)GetSettings("MiniSurveysEnabled");
                    if (ret == null) return true; // default disabled
                    return (ret == "1");
                }
                catch { return true; }
            }
            set { SetSettings("MiniSurveysEnabled", value ? "1" : "0"); }
        }

        public bool IdleEnabled
        {
            get
            {
                try
                {
                    var ret = (string)GetSettings("IdleEnabled");
                    if (ret == null) return true; // default enabled
                    return (ret == "1");
                }
                catch { return true; }
            }
            set { SetSettings("IdleEnabled", value ? "1" : "0"); }
        }

        public int MiniSurveyInterval
        {
            get
            {
                try { return int.Parse((string)GetSettings("MiniSurveyInterval")); }
                catch { return Settings.MiniSurveyIntervalDefaultValue; }
            }
            set { SetSettings("MiniSurveyInterval", value.ToString()); }
        }

        #endregion

        #region Visualization Queries

        /// <summary>
        /// Fetches the user's perceived productivity (and adds the start of the day with a default
        /// productivity value for visualization purposes)
        /// </summary>
        /// <param name="date"></param>
        /// <param name="inclusiveStartOfDay"></param>
        /// <returns></returns>
        public List<ProductivityTimeDto> GetUserProductivityData(DateTimeOffset date, bool inclusiveStartOfDay)
        {
            var prodList = new List<ProductivityTimeDto>();

            try
            {
                var reader = new SQLiteCommand(
                    "SELECT userProductivity, surveyStartTime FROM  " + Settings.UserEfficiencySurveyTable +
                    " WHERE STRFTIME('%s', DATE(time))==STRFTIME('%s', DATE('" + date.Date.ToString("u") + "')) ORDER BY surveyStartTime;", _connection).ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var ts = Helpers.JavascriptTimestampFromDateTime(DateTime.Parse((string)reader["surveyStartTime"]));
                        var prod = Convert.ToInt32(reader["userProductivity"]);

                        if (inclusiveStartOfDay && prodList.Count == 0) // first element
                        {
                            // add empty/default value
                            prodList.Add(new ProductivityTimeDto { UserProductvity = prod, Time = Helpers.JavascriptTimestampFromDateTime(GetUserWorkStart(date)) });
                        }

                        prodList.Add(new ProductivityTimeDto { UserProductvity = prod, Time = ts });
                    }
                }
                reader.Close();
            }
            catch (Exception e)
            {
                LogError(e.Message);
            }
            return prodList;
        }

        /// <summary>
        /// Gets a list of all the tasks the participant worked on and returns it as a formatted list
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public List<TasksWorkedOnTimeDto> GetTasksWorkedOnData(DateTimeOffset date)
        {
            var tasksList = new List<TasksWorkedOnTimeDto>();

            try
            {
                var reader = new SQLiteCommand(
                    "SELECT userTasksWorkedOn, surveyStartTime FROM  " + Settings.UserEfficiencySurveyTable +
                    " WHERE STRFTIME('%s', DATE(time))==STRFTIME('%s', DATE('" + date.Date.ToString("u") + "')) ORDER BY surveyStartTime;", _connection).ExecuteReader();

                if (reader.HasRows)
                {
                    // add empty/default value
                    tasksList.Add(new TasksWorkedOnTimeDto { TasksWorkedOn = 0, Time = Helpers.JavascriptTimestampFromDateTime(GetUserWorkStart(date)) });

                    while (reader.Read())
                    {
                        var ts = Helpers.JavascriptTimestampFromDateTime(DateTime.Parse((string)reader["surveyStartTime"]));
                        var tasksWorkedOn = ((string)reader["userTasksWorkedOn"]).Split(';').Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                        tasksList.Add(new TasksWorkedOnTimeDto { TasksWorkedOn = tasksWorkedOn.Count, Time = ts });
                    }
                }
                reader.Close();
            }
            catch (Exception e)
            {
                LogError(e.Message);
            }
            return tasksList;
        }

        /// <summary>
        /// Fetches the tasks the developer worked on for a given date, prepares it for the visualization
        /// and returns it as a dictionary (key = task, value = list of the start-end times the developer
        /// worked on each task)
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public Dictionary<string, List<StartEndTimeDto>> GetTaskGantTimelineData(DateTimeOffset date)
        {
            var taskList = new Dictionary<string, List<StartEndTimeDto>>();
            var previousSessionEndTs = Helpers.JavascriptTimestampFromDateTime(GetUserWorkStart(date));
            try
            {
                var reader = new SQLiteCommand(
                    "SELECT surveyStartTime as sessionEnd, userTasksWorkedOn FROM  " + Settings.UserEfficiencySurveyTable +
                    " WHERE STRFTIME('%s', DATE(time))==STRFTIME('%s', DATE('" + date.Date.ToString("u") + "')) ORDER BY surveyStartTime;", _connection).ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var startEndTimeObj = new StartEndTimeDto
                        {
                            StartTime = previousSessionEndTs,
                            EndTime = Helpers.JavascriptTimestampFromDateTime(DateTime.Parse((string)reader["sessionEnd"]))
                        };
                        previousSessionEndTs = startEndTimeObj.EndTime;

                        var tasksWorkedOn = ((string)reader["userTasksWorkedOn"]).Split(';').Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();

                        foreach (var task in tasksWorkedOn)
                        {
                            if (taskList.ContainsKey(task))
                            {
                                taskList[task].Add(startEndTimeObj);
                            }
                            else
                            {
                                var list = new List<StartEndTimeDto> { startEndTimeObj };
                                taskList.Add(task, list);
                            }
                        }
                    }
                }

                reader.Close();

                // add one task item if list is empty
                if (taskList.Count == 0)
                {
                    var startEndTimeObj = new StartEndTimeDto
                    {
                        StartTime = Helpers.JavascriptTimestampFromDateTime(GetUserWorkStart(date)),
                        EndTime = Helpers.JavascriptTimestampFromDateTime(GetUserWorkEnd(date))
                    };
                    taskList.Add(Dict.NoCategorizedTask, new List<StartEndTimeDto> { startEndTimeObj });
                }
                // add from last entry to present time if necessary
                else
                {
                    var startTs = GetUserLastMiniSurveyEntry(date);
                    var endTs = GetUserWorkEnd(date);
                    var dur = endTs - startTs;

                    if (dur.TotalMinutes >= 15)
                    {
                        var startEndTimeObj = new StartEndTimeDto
                        {
                            StartTime = Helpers.JavascriptTimestampFromDateTime(startTs),
                            EndTime = Helpers.JavascriptTimestampFromDateTime(endTs)
                        };

                        taskList.Add(Dict.NoCategorizedTask, new List<StartEndTimeDto> { startEndTimeObj });
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e.Message);
            }

            return taskList;
        }

        /// <summary>
        /// Fetches the activities a developer has on his computer for a given date and prepares the data
        /// to be visualized as a pie chart.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public Dictionary<string, long> GetActivityPieChartData(DateTimeOffset date)
        {
            var dto = new Dictionary<string, long>();

            try
            {
                // Hack 03.02.15 AM for Ventyx study participants
                var showIdleData = Database.GetInstanceSettings().IdleEnabled;
                var restrictQueryIdle = showIdleData ? "" : " AND process != 'IDLE'";

                var reader = new SQLiteCommand(
                    "SELECT process, IFNULL(COUNT(process), 0) AS count_process FROM  " + Settings.WindowsActivityTable +
                    " WHERE STRFTIME('%s', DATE(time))==STRFTIME('%s', DATE('" + date.Date.ToString("u") + "')) " + restrictQueryIdle + " GROUP BY process ORDER by process DESC;", _connection).ExecuteReader();

                if (!reader.HasRows)
                {
                    reader.Close();
                    return dto;
                }

                while (reader.Read())
                {
                    var process = (string)reader["process"];
                    var fileDesc = GetFileDescription(process);
                    var share = (long)reader["count_process"];

                    if (dto.ContainsKey(fileDesc))
                    {
                        dto[fileDesc] += share;
                    }
                    else
                    {
                        dto.Add(fileDesc, share);
                    }
                }

                reader.Close();

                return dto;
            }
            catch
            {
                return dto;
            }
        }

        /// <summary>
        /// Fetches all the activities and orders them according to a timeline
        /// TODO: add user input & window content
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public List<ActivitiesDto> GetActivitiesTimelineData(DateTimeOffset date)
        {
            var dto = new List<ActivitiesDto>();

            try
            {
                var reader = new SQLiteCommand(
                    "SELECT time, process, window FROM  " + Settings.WindowsActivityTable +
                    " WHERE STRFTIME('%s', DATE(time))==STRFTIME('%s', DATE('" + date.Date.ToString("u") + "')) ORDER BY time;", _connection).ExecuteReader();

                if (!reader.HasRows)
                {
                    reader.Close();
                    return dto;
                }

                // set up some vars for special cases
                var previousEntryAddedToDto = false;
                var firstNonIdleFound = false;
                ContextDto previousEntry = null;

                // go through database results
                while (reader.Read())
                {
                    var thisStartTs = Helpers.JavascriptTimestampFromDateTime(DateTime.Parse((string)reader["time"]));
                    var thisProcess = (string)reader["process"];
                    var thisWindowTitle = (string)reader["window"];

                    // avoid IDLE entries at start of day (e.g. if computer automatically starts up and syncs in the night)
                    if (firstNonIdleFound == false && thisProcess.Equals(Dict.Idle))
                    {
                        continue;
                    }
                    firstNonIdleFound = true;

                    if (previousEntry != null)
                    {
                        var diff = Helpers.DateTimeFromJavascriptTimestamp(thisStartTs) - Helpers.DateTimeFromJavascriptTimestamp(previousEntry.StartTime);
                        if (diff.TotalSeconds <= Settings.WindowsActivityCheckerInterval)
                        {
                            previousEntry.EndTime = thisStartTs;
                        }
                        else
                        {
                            var sT = Helpers.DateTimeFromJavascriptTimestamp(previousEntry.StartTime);
                            var add = sT.AddMilliseconds(Settings.WindowsActivityCheckerInterval);
                            previousEntry.EndTime = Helpers.JavascriptTimestampFromDateTime(add);
                            // TODO: there is some bug here (EndTime is 2h too small). Problem is in the two Helper functions DateTime <-> JavascriptTimestamp
                        }

                        // create dto from previous entry to save
                        var activityDto = new ActivitiesDto
                        {
                            Context = previousEntry.Context.Category,
                            StartTime = previousEntry.StartTime,
                            EndTime = previousEntry.EndTime
                        };
                        dto.Add(activityDto);

                        // create new entry
                        var thisEntry = new ContextDto
                        {
                            StartTime = thisStartTs,
                            Context = new ContextInfos { ProgramInUse = thisProcess, WindowTitle = thisWindowTitle }
                        };
                        thisEntry.Context.Category = ContextMapper.GetContextCategory(thisEntry);
                        previousEntry = thisEntry; // not yet add, as it's not complete
                        previousEntryAddedToDto = true;
                    }
                    // only first case
                    else
                    {
                        var thisEntry = new ContextDto
                        {
                            StartTime = thisStartTs,
                            Context = new ContextInfos { ProgramInUse = thisProcess, WindowTitle = thisWindowTitle }
                        };
                        thisEntry.Context.Category = ContextMapper.GetContextCategory(thisEntry);
                        previousEntry = thisEntry; // not yet add, as it's not complete
                    }
                }
                reader.Close();

                // last entry should also be stored
                //TODO: check if necessary/useful
                if (!previousEntryAddedToDto && previousEntry != null)
                {
                    var activitydto = new ActivitiesDto
                    {
                        Context = previousEntry.Context.Category,
                        StartTime = previousEntry.StartTime,
                        EndTime = Helpers.JavascriptTimestampFromDateTime(GetUserWorkEnd(date))
                    };
                    dto.Add(activitydto);
                }

                //foreach (var item in dto)
                //{
                //    Console.WriteLine(item.Context + "\t" + Helpers.DateTimeFromJavascriptTimestamp(item.StartTime).TimeOfDay + " - " + Helpers.DateTimeFromJavascriptTimestamp(item.EndTime).TimeOfDay);
                //}

                return dto;
            }
            catch
            {
                return dto;
            }
        }

        /// <summary>
        /// Returns a dictionary with an input-level like data set for each interval (Settings.UserInputVisMinutesInterval)
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public Dictionary<DateTime, int> GetUserInputTimelineData(DateTimeOffset date)
        {
            var dto = new Dictionary<DateTime, int>();

            try
            {
                var keyboardReader = new SQLiteCommand("SELECT timestamp FROM " + Settings.UserInputKeyboardTable +
                    " WHERE STRFTIME('%s', DATE(time))==STRFTIME('%s', DATE('" + date.Date.ToString("u") + "'));", _connection).ExecuteReader();

                var mouseClickReader = new SQLiteCommand("SELECT timestamp FROM " + Settings.UserInputMouseClickTable +
                    " WHERE STRFTIME('%s', DATE(time))==STRFTIME('%s', DATE('" + date.Date.ToString("u") + "'));", _connection).ExecuteReader();

                var mouseScrollingReader = new SQLiteCommand("SELECT timestamp, scrollDelta FROM " + Settings.UserInputMouseScrollingTable +
                    " WHERE STRFTIME('%s', DATE(time))==STRFTIME('%s', DATE('" + date.Date.ToString("u") + "')) AND scrollDelta > 0;", _connection).ExecuteReader();

                var mouseMovementReader = new SQLiteCommand("SELECT timestamp, movedDistance FROM " + Settings.UserInputMouseMovementTable +
                    " WHERE STRFTIME('%s', DATE(time))==STRFTIME('%s', DATE('" + date.Date.ToString("u") + "')) AND movedDistance > 0;", _connection).ExecuteReader();

                if (!keyboardReader.HasRows && !mouseClickReader.HasRows && !mouseMovementReader.HasRows && mouseScrollingReader.HasRows)
                {
                    keyboardReader.Close();
                    mouseClickReader.Close();
                    mouseScrollingReader.Close();
                    mouseMovementReader.Close();
                    return dto;
                }

                // 1. prepare Dictionary
                PrepareTimeAxis(date, dto, Settings.UserInputVisMinutesInterval);

                // 2. fill keyboard data
                while (keyboardReader.Read())
                {
                    var time = DateTime.Parse((string)keyboardReader["timestamp"]);
                    time = time.AddSeconds(-time.Second); // nice seconds

                    // find 15 minutes interval
                    time = Helpers.RoundUp(time, TimeSpan.FromMinutes(Settings.UserInputVisMinutesInterval));

                    // add keystroke
                    if (dto.ContainsKey(time)) dto[time]++;
                }

                // 3. fill mouse click data
                while (mouseClickReader.Read())
                {
                    var time = DateTime.Parse((string)mouseClickReader["timestamp"]);
                    time = time.AddSeconds(-time.Second); // nice seconds

                    // find 10 minutes interval
                    time = Helpers.RoundUp(time, TimeSpan.FromMinutes(Settings.UserInputVisMinutesInterval));

                    // add mouse click (with weighting)
                    if (dto.ContainsKey(time)) dto[time] += Settings.MouseClickKeyboardRatio;
                }

                // 4. fill mouse scrolling data
                while (mouseScrollingReader.Read())
                {
                    var time = DateTime.Parse((string)mouseScrollingReader["timestamp"]);
                    time = time.AddSeconds(-time.Second); // nice seconds

                    // find 10 minutes interval
                    time = Helpers.RoundUp(time, TimeSpan.FromMinutes(Settings.UserInputVisMinutesInterval));

                    // add mouse scrolling (with weighting)
                    var scroll = (long)mouseScrollingReader["scrollDelta"];
                    if (scroll > 0 && dto.ContainsKey(time)) dto[time] += (int)Math.Round(scroll * Settings.MouseScrollingKeyboardRatio, 0);
                }

                // 5. fill mouse move data
                while (mouseMovementReader.Read())
                {
                    var time = DateTime.Parse((string)mouseMovementReader["timestamp"]);
                    time = time.AddSeconds(-time.Second); // nice seconds

                    // find 10 minutes interval
                    time = Helpers.RoundUp(time, TimeSpan.FromMinutes(Settings.UserInputVisMinutesInterval));

                    // add mouse movement (with weighting)
                    var moved = (long)mouseMovementReader["movedDistance"];
                    if (moved > 0 && dto.ContainsKey(time)) dto[time] += (int)Math.Round(moved * Settings.MouseMovementKeyboardRatio, 0);
                }

                keyboardReader.Close();
                mouseClickReader.Close();
                mouseScrollingReader.Close();
                mouseMovementReader.Close();

                return dto;
            }
            catch (Exception e)
            {
                LogError(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Prepares the timeline axis,
        /// setting the first and last entry of the workday as start and end point
        /// </summary>
        /// <param name="date"></param>
        /// <param name="dto"></param>
        /// <param name="interval"></param>
        public void PrepareTimeAxis(DateTimeOffset date, Dictionary<DateTime, int> dto, int interval)
        {
            var min = GetUserWorkStart(date);
            min = min.AddSeconds(-min.Second); // nice seconds
            min = Helpers.RoundUp(min, TimeSpan.FromMinutes(-interval)); // nice minutes

            var max = GetUserWorkEnd(date); //GetUserLastMiniSurveyEntry(date);
            max = max.AddSeconds(-max.Second); // nice seconds
            max = Helpers.RoundUp(max, TimeSpan.FromMinutes(interval)); // nice minutes

            while (min < max)
            {
                var key = min.AddMinutes(interval);
                dto.Add(key, 0);
                min = key;
            }
        }

        /// <summary>
        /// Gets the file description of a proces and returns it formatted
        /// (shortened if neccessary)
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        private static string GetFileDescription(string process)
        {
            //var fileDesc = (process == Dict.Idle ? Dict.Idle : GetFileDescriptionFromProcess(process));
            var fileDesc = process; //TODO: enable file description

            // shorten file description if necessary
            if (fileDesc == null)
            {
                fileDesc = process;
                if (fileDesc.Length > 30)
                    fileDesc = "..." + fileDesc.Substring(fileDesc.Length - 27);
            }
            else if (fileDesc.Length > 30)
            {
                fileDesc = fileDesc.Substring(0, 27) + "...";
            }

            return fileDesc;
        }

        /// <summary>
        /// Fetches and calculates the total hours a developer worked on for a given date
        /// (based on the first non-idle entry).
        /// TODO: make more accurate by removing IDLE time
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public double GetTotalHoursWorked(DateTimeOffset date)
        {
            var totalHours = 0.0;
            var firstEntryDateTime = DateTime.Now;
            var lastEntryDateTime = DateTime.Now;

            firstEntryDateTime = GetUserWorkStart(date);
            lastEntryDateTime = GetUserWorkEnd(date);

            totalHours = lastEntryDateTime.TimeOfDay.TotalHours - firstEntryDateTime.TimeOfDay.TotalHours;
            return Math.Round(totalHours, 1);
        }

        public DateTime GetUserWorkStart(DateTimeOffset date)
        {
            var firstEntryDateTime = DateTime.Now; // default value
            try
            {
                var firstEntryReader = new SQLiteCommand("SELECT time FROM " + Settings.WindowsActivityTable +
                                                         " WHERE STRFTIME('%s', DATE(time))==STRFTIME('%s', DATE('" +
                                                         date.Date.ToString("u") + "'))" +
                                                         " AND process != '" + Dict.Idle +
                                                         "' ORDER BY time ASC LIMIT 1;", _connection).ExecuteReader();

                if (firstEntryReader.HasRows)
                {
                    firstEntryReader.Read(); // read only once
                    firstEntryDateTime = DateTime.Parse((string)firstEntryReader["time"]);
                }

                firstEntryReader.Close();
            }
            catch (Exception e)
            {
                LogError(e.Message);
            }
            return firstEntryDateTime;
        }

        public DateTime GetUserWorkEnd(DateTimeOffset date)
        {
            var lastEntryDateTime = DateTime.Now;
            try
            {
                var lastEntryReader = new SQLiteCommand("SELECT time FROM " + Settings.WindowsActivityTable +
                                                        " WHERE STRFTIME('%s', DATE(time))==STRFTIME('%s', DATE('" +
                                                        date.Date.ToString("u") + "'))" +
                                                        " AND process != '" + Dict.Idle + "' ORDER BY time DESC LIMIT 1;",
                    _connection).ExecuteReader();

                if (lastEntryReader.HasRows)
                {

                    lastEntryReader.Read(); // read only once
                    lastEntryDateTime = DateTime.Parse((string)lastEntryReader["time"]);
                }

                lastEntryReader.Close();
            }
            catch (Exception e)
            {
                LogError(e.Message);
            }
            return lastEntryDateTime;
        }

        private DateTime GetUserLastMiniSurveyEntry(DateTimeOffset date)
        {
            var lastEntryDateTime = DateTime.Now;
            try
            {
                var lastEntryReader = new SQLiteCommand("SELECT time FROM " + Settings.UserEfficiencySurveyTable +
                                                        " WHERE STRFTIME('%s', DATE(time))==STRFTIME('%s', DATE('" +
                                                        date.Date.ToString("u") + "')) ORDER BY time DESC LIMIT 1;",
                    _connection).ExecuteReader();

                if (lastEntryReader.HasRows)
                {

                    lastEntryReader.Read(); // read only once
                    lastEntryDateTime = DateTime.Parse((string)lastEntryReader["time"]);
                }

                lastEntryReader.Close();
            }
            catch (Exception e)
            {
                LogError(e.Message);
            }
            return lastEntryDateTime;
        }

        /// <summary>
        /// gets the name of the process
        /// TODO: currently doesn't work
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        internal string GetFileDescriptionFromProcess(string process)
        {
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(process);
                var res = (versionInfo.FileDescription == string.Empty) ? null : versionInfo.FileDescription;
                return res;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        #endregion

        #region Other Database stuff (Connect, Disconnect, create Log table, Singleton, etc.)

        public DatabaseImplementation(string dbFilePath)
        {
            CurrentDatabaseDumpFile = dbFilePath;
        }

        /// <summary>
        /// Opens a connection to the database with the current database save path
        /// </summary>
        public void Connect()
        {
            // Open the Database connection
            _connection = new SQLiteConnection("Data Source=" + CurrentDatabaseDumpFile);
            _connection.Open();

            // Create log table if it doesn't exist
            CreateLogTable();

            LogInfo(string.Format("Opened the connection to the database (File={0}).", CurrentDatabaseDumpFile));
        }

        public void Reconnect(string dbFilePath)
        {
            CurrentDatabaseDumpFile = dbFilePath;
            Connect();
        }

        /// <summary>
        /// Closes the connection to the database.
        /// </summary>
        public void Disconnect()
        {
            LogInfo(string.Format("Closed the connection to the database (File={0}).", CurrentDatabaseDumpFile));
            if (_connection == null || _connection.State == ConnectionState.Closed) return;
            _connection.Close();
        }

        /// <summary>
        /// Creates a table for the log inputs (if it doesn't yet exist)
        /// </summary>
        public void CreateLogTable()
        {
            try 
            {
                const string query = "CREATE TABLE IF NOT EXISTS " + Settings.LogDbTable + " (id INTEGER PRIMARY KEY, created INTEGER, message TEXT, type TEXT)";
                ExecuteDefaultQuery(query);
            } 
            catch (Exception e) 
            {
                Shared.Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Creates a table for the settings (keys) (if it doesn't yet exist)
        /// </summary>
        public void CreateSettingsTable()
        {
            try 
            {
                const string query = "CREATE TABLE IF NOT EXISTS " + Settings.SettingsDbTable + " (id INTEGER PRIMARY KEY, key TEXT, value TEXT)";
                ExecuteDefaultQuery(query);
            } 
            catch (Exception e) 
            {
                Shared.Logger.WriteToLogFile(e);
            }
        }

        #endregion
    }
}
