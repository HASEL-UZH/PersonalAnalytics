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
        internal static List<Tuple<string, DateTime, int, int>> GetMeetingsFromDatabase(DateTimeOffset date)
        {
            var meetings = new List<Tuple<string, DateTime, int, int>>();
            try
            {
                var query = "SELECT subject, time, durationInMins, numAttendees FROM " + Settings.MeetingsTable + " "
                            + "WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date) + " "
                            + "AND subject != '" + Dict.Anonymized + "';";

                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    var subject = (string)row["subject"];
                    var time = DateTime.Parse((string)row["time"], CultureInfo.InvariantCulture);
                    var duration = (row["durationInMins"] == DBNull.Value) ? 0 : Convert.ToInt32(row["durationInMins"], CultureInfo.InvariantCulture);
                    var numAttendess = (row["numAttendees"] == DBNull.Value) ? 0 : Convert.ToInt32(row["numAttendees"], CultureInfo.InvariantCulture);

                    var t = new Tuple<string, DateTime, int, int>(subject, time, duration, numAttendess);
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
                          + "SELECT process, (strftime('%s', tsEnd) - strftime('%s', tsStart)) as 'difference' "
                          + "FROM " + Settings.WindowsActivityTable + " "
                          + "WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, "tsStart") + " AND " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, "tsEnd") + " "
                          + "GROUP BY id, tsStart"
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
