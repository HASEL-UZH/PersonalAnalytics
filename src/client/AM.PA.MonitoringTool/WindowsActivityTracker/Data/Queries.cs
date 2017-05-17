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
        internal static void CreateWindowsActivityTable()
        {
            try
            {
                Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.DbTable + " (id INTEGER PRIMARY KEY, time TEXT, window TEXT, process TEXT)");
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Saves the timestamp, process name and window title into the database.
        /// 
        /// In case the user doesn't want the window title to be stored (For privacy reasons),
        /// it is obfuscated.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="process"></param>
        internal static void InsertSnapshot(string window, string process)
        {
            if (Shared.Settings.AnonymizeSensitiveData)
            {
                var dto = new ContextDto { Context = new ContextInfos { ProgramInUse = process, WindowTitle = window } };
                window = Dict.Anonymized + " " + ContextMapper.GetContextCategory(dto);  // obfuscate window title
            }

            Database.GetInstance().ExecuteDefaultQuery("INSERT INTO " + Settings.DbTable + " (time, window, process) VALUES (strftime('%Y-%m-%d %H:%M:%f', 'now', 'localtime'), " +
                Database.GetInstance().Q(window) + ", " + Database.GetInstance().Q(process) + ")");
        }

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
                    var thisWindowTitle = (string)row["window"];
                    e.WindowTitles.Add(thisWindowTitle);

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
                            lastItem.WindowTitles.Add(thisWindowTitle);
                        }
                        // previous item is same, update it (duration and tsEnd)
                        else if (!mapToActivity && e.ProcessName == _previousWindowsActivityEntry.ProcessName)
                        {
                            var lastItem = orderedActivityList.Last();
                            lastItem.DurationInSeconds += e.DurationInSeconds;
                            lastItem.EndTime = e.EndTime;
                            lastItem.WindowTitles.Add(thisWindowTitle);
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
    }
}
