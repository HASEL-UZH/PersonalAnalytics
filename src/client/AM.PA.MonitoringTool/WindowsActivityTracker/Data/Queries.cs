// Created by André Meyer at MSR
// Created: 2015-11-12
// 
// Licensed under the MIT License.
using Shared;
using Shared.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Globalization;
using WindowsActivityTracker.Helpers;
using WindowsActivityTracker.Models;

namespace WindowsActivityTracker.Data
{
    public class Queries
    {

        private static string QUERY_CREATE = "CREATE TABLE IF NOT EXISTS " + Settings.DbTable + " (id INTEGER PRIMARY KEY, time TEXT, tsStart TEXT, tsEnd TEXT, window TEXT, process TEXT);";
        private static string QUERY_INDEX = "CREATE INDEX IF NOT EXISTS windows_activity_ts_start_idx ON " + Settings.DbTable + " (tsStart);";
        private static string QUERY_INSERT = "INSERT INTO " + Settings.DbTable + " (time, tsStart, tsEnd, window, process) VALUES ({0}, {1}, {2}, {3}, {4});";

        #region Daemon Queries

        internal static void CreateWindowsActivityTable()
        {
            try
            {
                Database.GetInstance().ExecuteDefaultQuery(QUERY_CREATE);
                Database.GetInstance().ExecuteDefaultQuery(QUERY_INDEX); // add index
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        internal static void UpdateDatabaseTables(int version)
        {
            try
            {
                // database update 2017-07-20 (added two columns to 'windows_activity' table: tsStart and tsEnd)
                // need to migrate the existing values in the table
                if (version == 4)
                {
                    if (Database.GetInstance().HasTable(Settings.DbTable))
                    {
                        // update table: add columns & index
                        Database.GetInstance().ExecuteDefaultQuery("ALTER TABLE " + Settings.DbTable + " ADD COLUMN tsStart TEXT;");
                        Database.GetInstance().ExecuteDefaultQuery("ALTER TABLE " + Settings.DbTable + " ADD COLUMN tsEnd TEXT;");
                        Database.GetInstance().ExecuteDefaultQuery(QUERY_INDEX);

                        // migrate data (set tsStart / tsEnd)

                        // TODO: implement
                    }
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }


        /// <summary>
        /// Saves the timestamp, start and end, process name and window title into the database.
        /// 
        /// In case the user doesn't want the window title to be stored (For privacy reasons),
        /// it is obfuscated.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="process"></param>
        internal static void InsertSnapshot(WindowsActivityEntry entry)
        {
            try
            {
                if (Shared.Settings.AnonymizeSensitiveData)
                {
                    var dto = new ContextDto { Context = new ContextInfos { ProgramInUse = entry.Process, WindowTitle = entry.WindowTitle } };
                    entry.WindowTitle = Dict.Anonymized + " " + ContextMapper.GetContextCategory(dto);  // obfuscate window title
                }

                var tsEndString = (entry.TsEnd == DateTime.MinValue) ? string.Empty : Database.GetInstance().QTime2(entry.TsEnd);

                var query = string.Format(QUERY_INSERT,
                                          "strftime('%Y-%m-%d %H:%M:%f', 'now', 'localtime')",
                                          Database.GetInstance().QTime2(entry.TsStart),
                                          tsEndString,
                                          Database.GetInstance().Q(entry.WindowTitle),
                                          Database.GetInstance().Q(entry.Process));

                Database.GetInstance().ExecuteDefaultQuery(query);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        internal static bool UserInputTableExists()
        {
            var res = Database.GetInstance().HasTable(Shared.Settings.UserInputTable);
            return res;
        }

        /// <summary>
        /// Returns a list with tsStart and tsEnd of all missed sleep events
        /// </summary>
        /// <param name="ts_checkFrom"></param>
        /// <param name="ts_checkTo"></param>
        /// <returns></returns>
        internal static List<Tuple<DateTime, DateTime>> GetMissedSleepEvents(DateTime ts_checkFrom, DateTime ts_checkTo)
        {
            var results = new List<Tuple<DateTime, DateTime>>();

            try
            {
                var query = "SELECT wa1.time as 'tsFrom', wa2.time as 'tsTo', ( "
	                      + "SELECT sum(ui.keyTotal) + sum(ui.clickTotal) + sum(ui.ScrollDelta) + sum(ui.movedDistance) "
                          + "FROM " + Shared.Settings.UserInputTable + " as ui "
                          + "WHERE (ui.tsStart between wa1.time and wa2.time) AND (ui.tsEnd between wa1.time and wa2.time) "
                          + ") as 'sumUserInput' "
                          + "FROM " + Settings.DbTable + " wa1 INNER JOIN " + Settings.DbTable + " wa2 on wa1.id + 1 == wa2.id "
                          + "WHERE wa1.process <> '" + Dict.Idle + "' " // we are looking for cases where the IDLE event was not catched
                          + "AND wa1.process <> 'skype' AND wa1.process <> 'lync' " // IDLE during calls are okay
                          + "AND (wa1.time between "+ Database.GetInstance().QTime(ts_checkFrom) + " AND " + Database.GetInstance().QTime(ts_checkTo) + ") " // perf
                          + "AND (strftime('%s', wa2.time) - strftime('%s', wa1.time)) > " + Settings.IdleSleepValidate_ThresholdIdleBlocks_s + ";"; // IDLE time window we are looking for

                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    if (row["sumUserInput"] == DBNull.Value || Convert.ToInt32(row["sumUserInput"]) == 0)
                    {
                        var tsFrom = DateTime.Parse((string)row["tsFrom"], CultureInfo.InvariantCulture);
                        var tsTo = DateTime.Parse((string)row["tsTo"], CultureInfo.InvariantCulture);
                        var pair = new Tuple<DateTime, DateTime>(tsFrom, tsTo);
                        results.Add(pair);
                    }
                }
                table.Dispose();
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return results;
        }

        internal static void AddMissedSleepIdleEntry(List<Tuple<DateTime, DateTime>> toFix)
        {
            foreach (var item in toFix)
            {
                var idleTimeFix = item.Item1.AddMilliseconds(Settings.NotCountingAsIdleInterval_ms);
                var tsEnd = item.Item2;

                var tempItem = new WindowsActivityEntry(idleTimeFix, tsEnd, Settings.ManualSleepIdle, Dict.Idle, IntPtr.Zero); //TODO: enable again
                Logger.WriteToLogFile(new Exception(Settings.ManualSleepIdle + " from: " + item + " to: " + idleTimeFix));
            }
        }

        #endregion

        #region Visualization Queries

        /// <summary>
        /// Returns the program where the user focused on the longest
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal static FocusedWorkDto GetLongestFocusOnProgram(DateTimeOffset date)
        {
            try
            {
                var query = "SELECT t1.process as 'process', (strftime('%s', t2.time) - strftime('%s', t1.time)) as 'difference', t1.time as 'from', t2.time as 'to' " // t1.window as 'window', 
                          + "FROM " + Settings.DbTable + " t1 LEFT JOIN " + Settings.DbTable + " t2 on t1.id + 1 = t2.id "
                          + "WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, "t1.time") + " and " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, "t2.time") + " "
                          + "AND t1.process <> '" + Dict.Idle + "' "
                          + "GROUP BY t1.id, t1.time "
                          + "ORDER BY difference DESC "
                          + "LIMIT 1;";

                var table = Database.GetInstance().ExecuteReadQuery(query);

                if (table != null && table.Rows.Count == 1)
                {
                    var row = table.Rows[0];
                    var process = Shared.Helpers.ProcessNameHelper.GetFileDescriptionFromProcess((string)row["process"]);
                    //var window = (string)row["window"];
                    var difference = Convert.ToInt32(row["difference"], CultureInfo.InvariantCulture);
                    var from = DateTime.Parse((string)row["from"], CultureInfo.InvariantCulture);
                    var to = DateTime.Parse((string)row["to"], CultureInfo.InvariantCulture);

                    table.Dispose();
                    return new FocusedWorkDto(process, difference, from, to);
                }
                else
                {
                    table.Dispose();
                    return null;
                }  
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);

                return null;
            }
        }

        /// <summary>
        /// Fetches the activities a developer has on his computer for a given date in an
        /// ordered list according to time.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal static List<WindowsActivity> GetDayTimelineData(DateTimeOffset date, bool mapToActivity)
        {
            var orderedActivityList = new List<WindowsActivity>();

            try
            {
                var query = "SELECT t1.time as 'tsStart', t2.time as 'tsEnd', t1.window, t1.process, (strftime('%s', t2.time) - strftime('%s', t1.time)) as 'durInSec' " //t1.id, t1.time as 'from', t2.time as 'to'
                              + "FROM " + Settings.DbTable + " t1 LEFT JOIN " + Settings.DbTable + " t2 on t1.id + 1 = t2.id "
                              + "WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, "t1.time") + " and " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, "t2.time") + " "
                              + "ORDER BY t1.time;";

                var table = Database.GetInstance().ExecuteReadQuery(query);

                WindowsActivity _previousWindowsActivityEntry = null;

                foreach (DataRow row in table.Rows)
                {
                    // fetch items from database
                    var e = new WindowsActivity();
                    e.StartTime = DateTime.Parse((string)row["tsStart"], CultureInfo.InvariantCulture);
                    e.EndTime = DateTime.Parse((string)row["tsEnd"], CultureInfo.InvariantCulture);
                    e.DurationInSeconds = row.IsNull("durInSec") ? 0 : Convert.ToInt32(row["durInSec"], CultureInfo.InvariantCulture);
                    e.ProcessName = (string)row["process"];
                    e.WindowTitle = (string)row["window"];

                    // if the user wishes to see activity categories rather than processes
                    // map it automatically
                    if (mapToActivity)
                    {
                        ProcessToActivityMapper.Map(e);
                    }

                    // check if we add a new item, or merge with the previous one
                    if (_previousWindowsActivityEntry != null)
                    {
                        // previous item is same, update it (duration and tsEnd)
                        if (mapToActivity && e.ActivityCategory == _previousWindowsActivityEntry.ActivityCategory)
                        {
                            var lastItem = orderedActivityList.Last();
                            lastItem.DurationInSeconds += e.DurationInSeconds;
                            lastItem.EndTime = e.EndTime;
                        }
                        // previous item is same, update it (duration and tsEnd)
                        else if (!mapToActivity && e.ProcessName == _previousWindowsActivityEntry.ProcessName)
                        {
                            var lastItem = orderedActivityList.Last();
                            lastItem.DurationInSeconds += e.DurationInSeconds;
                            lastItem.EndTime = e.EndTime;
                        }
                        // previous item is different, add it to list
                        else
                        {
                            orderedActivityList.Add(e);
                        }
                    }
                    else // first item
                    {
                        orderedActivityList.Add(e);
                    }
                    _previousWindowsActivityEntry = e;
                }
                table.Dispose();
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return orderedActivityList;
        }

        /// <summary>
        /// Fetches the activities a developer has on his computer for a given date and prepares the data
        /// to be visualized as a pie chart.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal static Dictionary<string, double> GetActivityPieChartData(DateTimeOffset date)
        {
            var dto = new Dictionary<string, double>();

            try
            {
                var query = "SELECT process, sum(difference) / 60.0 / 60.0  as 'durInHrs' "
                          + "FROM (	"
                          + "SELECT t1.process, (strftime('%s', t2.time) - strftime('%s', t1.time)) as 'difference' " //t1.id, t1.time as 'from', t2.time as 'to'
                          + "FROM " + Settings.DbTable + " t1 LEFT JOIN " + Settings.DbTable + " t2 on t1.id + 1 = t2.id "
                          + "WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, "t1.time") + " and " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, "t2.time") + " "
                          + "GROUP BY t1.id, t1.time "
                          + ") "
                          + "WHERE difference > 0 and process <> '" + Dict.Idle + "' "
                          + "GROUP BY process;";

                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    var process = (string)row["process"];
                    var fileDesc = Shared.Helpers.ProcessNameHelper.GetFileDescription(process);
                    var share = (double)row["durInHrs"];

                    if (dto.ContainsKey(fileDesc))
                    {
                        dto[fileDesc] += share;
                    }
                    else
                    {
                        dto.Add(fileDesc, share);
                    }
                }
                table.Dispose();
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return dto;
        }

        #endregion
    }
}
