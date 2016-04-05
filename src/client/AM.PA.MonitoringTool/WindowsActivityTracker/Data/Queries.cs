// Created by André Meyer at MSR
// Created: 2015-11-12
// 
// Licensed under the MIT License.
using Shared;
using Shared.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;

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
