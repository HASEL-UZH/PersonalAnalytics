// Created by André Meyer at MSR
// Created: 2015-12-16
// 
// Licensed under the MIT License.

using Shared;
using Shared.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace TimeSpentVisualizer.Data
{
    internal class Queries
    {
        /// <summary>
        /// Loads a list of meetings for a given date from the database
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal static List<Tuple<string, DateTime, int>> GetMeetingsFromDatabase(DateTimeOffset date)
        {
            var meetings = new List<Tuple<string, DateTime, int>>();
            try
            {
                var query = "SELECT subject, time, durationInMins FROM " + Settings.MeetingsTable + " "
                            + "WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date) + " "
                            + "AND subject != '" + Dict.Anonymized + "';";

                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    var subject = (string)row["subject"];
                    var time = DateTime.Parse((string)row["time"], CultureInfo.InvariantCulture);
                    var duration = Convert.ToInt32(row["durationInMins"], CultureInfo.InvariantCulture);

                    var t = new Tuple<string, DateTime, int>(subject, time, duration);
                    meetings.Add(t);
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return meetings;
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
                var query = "SELECT process, sum(difference) / 60.0  as 'durInMins' "
                          + "FROM (	"
                          + "SELECT t1.process, (strftime('%s', t2.time) - strftime('%s', t1.time)) as 'difference' " //t1.id, t1.time as 'from', t2.time as 'to'
                          + "FROM " + Settings.WindowsActivityTable + " t1 LEFT JOIN " + Settings.WindowsActivityTable + " t2 on t1.id + 1 = t2.id "
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
                    var share = (double)row["durInMins"];

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
