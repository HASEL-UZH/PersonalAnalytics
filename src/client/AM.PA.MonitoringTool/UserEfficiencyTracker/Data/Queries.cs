using Shared;
using Shared.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using UserEfficiencyTracker.Models;

namespace UserEfficiencyTracker.Data
{
    public class Queries
    {
        /// <summary>
        /// AM: added some empty fields for additional questions in the future
        /// </summary>
        internal static void CreateUserEfficiencyTables()
        {
            Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.DbTableIntervalPopup + " (id INTEGER PRIMARY KEY, time TEXT, surveyNotifyTime TEXT, surveyStartTime TEXT, surveyEndTime TEXT, userProductivity NUMBER, column1 TEXT, column2 TEXT, column3 TEXT, column4 TEXT, column5 TEXT, column6 TEXT, column7 TEXT, column8 TEXT )");
            Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.DbTableDailyPopUp + " (id INTEGER PRIMARY KEY, time TEXT, workDay TEXT, surveyNotifyTime TEXT, surveyStartTime TEXT, surveyEndTime TEXT, userProductivity NUMBER, column1 TEXT, column2 TEXT, column3 TEXT, column4 TEXT, column5 TEXT, column6 TEXT, column7 TEXT, column8 TEXT )");
        }

        /// <summary>
        /// Saves the survey entry to the database
        /// </summary>
        /// <param name="entry"></param>
        internal static void SaveIntervalEntry(SurveyEntry entry)
        {
            if (entry == null) return;

            try
            {
                var query = "INSERT INTO " + Settings.DbTableIntervalPopup + " (time, surveyNotifyTime, surveyStartTime, surveyEndTime, userProductivity) VALUES " +
                            "(strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'), " +
                            Database.GetInstance().QTime(entry.TimeStampNotification) + ", " +
                            Database.GetInstance().QTime(entry.TimeStampStarted) + ", " +
                            Database.GetInstance().QTime(entry.TimeStampFinished) + ", " +
                            Database.GetInstance().Q(entry.Productivity.ToString(CultureInfo.InvariantCulture)) + ");";

                Database.GetInstance().ExecuteDefaultQuery(query);
            }
            catch { }
        }

        /// <summary>
        /// Saves the survey entry to the database
        /// </summary>
        /// <param name="entry"></param>
        internal static void SaveDailyEntry(SurveyEntry entry)
        {
            if (entry == null) return;

            try
            {
                var query = "INSERT INTO " + Settings.DbTableDailyPopUp + " (time, workDay, surveyNotifyTime, surveyStartTime, surveyEndTime, userProductivity) VALUES " +
                            "(strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'), " +
                            Database.GetInstance().QTime(entry.PreviousWorkDay) + ", " +
                            Database.GetInstance().QTime(entry.TimeStampNotification) + ", " +
                            Database.GetInstance().QTime(entry.TimeStampStarted) + ", " +
                            Database.GetInstance().QTime(entry.TimeStampFinished) + ", " +
                            Database.GetInstance().Q(entry.Productivity.ToString(CultureInfo.InvariantCulture)) + ");";

                Database.GetInstance().ExecuteDefaultQuery(query);
            }
            catch { }
        }

        /// <summary>
        /// returns the previous survey entry if available
        /// (only get interval survey responses)
        /// </summary>
        /// <returns>previous survey entry or null, if there isn't any</returns>
        internal static SurveyEntry GetPreviousIntervalSurveyEntry()
        {
            var res = Database.GetInstance().ExecuteReadQuery("SELECT surveyNotifyTime, surveyStartTime, surveyEndTime, userProductivity FROM " + Settings.DbTableDailyPopUp + "' ORDER BY time DESC;");
            if (res == null || res.Rows.Count == 0) return null;

            var entry = new SurveyEntry();

            if (res.Rows[0]["surveyNotifyTime"] != null)
            {
                try
                {
                    var val = DateTime.Parse((string)res.Rows[0]["surveyNotifyTime"], CultureInfo.InvariantCulture);
                    entry.TimeStampNotification = val;
                }
                catch { } // necessary, if we run it after the DB initialization, there is no value
            }
            if (res.Rows[0]["surveyStartTime"] != null)
            {
                try
                {
                    var val = DateTime.Parse((string)res.Rows[0]["surveyStartTime"], CultureInfo.InvariantCulture);
                    entry.TimeStampStarted = val;
                }
                catch { } // necessary, if we run it after the DB initialization, there is no value
            }
            if (res.Rows[0]["surveyEndTime"] != null)
            {
                try
                {
                    var val = DateTime.Parse((string)res.Rows[0]["surveyEndTime"], CultureInfo.InvariantCulture);
                    entry.TimeStampFinished = val;
                }
                catch { } // necessary, if we run it after the DB initialization, there is no value
            }
            if (res.Rows[0]["userProductivity"] != null)
            {
                try
                {
                    var val = Convert.ToInt32(res.Rows[0]["userProductivity"], CultureInfo.InvariantCulture);
                    entry.Productivity = val;
                }
                catch { } // necessary, if we run it after the DB initialization, there is no value
            }
            return entry;
        }

        /// <summary>
        /// returns the previous survey entry if available
        /// (only get daily survey responses)
        /// </summary>
        /// <returns>previous survey entry or null, if there isn't any</returns>
        internal static DateTime GetLastPopUpResponse()
        {
            var date = DateTime.MinValue;
            var query = "SELECT date(time) as 'date' FROM " + Settings.DbTableIntervalPopup + " ORDER BY time DESC LIMIT 1;";
            var table = Database.GetInstance().ExecuteReadQuery(query);

            try
            {
                if (table != null && table.Rows.Count == 1)
                {
                    var row = table.Rows[0];
                    date = DateTime.Parse((string)row["date"], CultureInfo.InvariantCulture);
                }
                else
                {
                    table.Dispose();
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
            finally
            {
                table.Dispose();
            }

            return date;
        }

        /// <summary>
        /// Get Previous Work Day
        /// </summary>
        /// <returns></returns>
        internal static DateTime GetPreviousActiveWorkDay()
        {
            var date = DateTime.MinValue;

            var query = "SELECT date FROM ( "
                        + "SELECT date(time) as 'date', count(*) as 'sumInSec' "
                        + "FROM windows_activity "
                        + "WHERE process <> 'IDLE' AND date(time) <> " + Database.GetInstance().QDate(DateTime.Now) + " "
                        + "GROUP BY date(time) "
                        + "ORDER BY date(time) DESC "
                        + ") WHERE sumInSec > 600 "
                        + "LIMIT 1;";

            var table = Database.GetInstance().ExecuteReadQuery(query);

            try
            {
                if (table != null && table.Rows.Count == 1)
                {
                    var row = table.Rows[0];
                    date = DateTime.Parse((string)row["date"], CultureInfo.InvariantCulture);
                }
                else
                {
                    table.Dispose();
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
            finally
            {
                table.Dispose();
            }

            return date;
        }

        /// <summary>
        /// Returns a dictionary with the productivity on a timeline
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal static List<Tuple<DateTime, int>> GetUserProductivityTimelineData(DateTimeOffset date, VisType type, bool withNonWork = false)
        {
            var prodList = new List<Tuple<DateTime, int>>();

            try
            {
                var filterNonWork = (withNonWork) ? "" :" AND userProductivity <> -1 ";

                var query = "SELECT userProductivity, surveyEndTime FROM " + Settings.DbTableIntervalPopup + " " + // end time is the time the participant answered
                                      "WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(type, date, "surveyNotifyTime") + " " + // only show perceived productivity values for the day
                                      filterNonWork +
                                      " ORDER BY surveyEndTime;";
                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    var time = DateTime.Parse((string)row["surveyEndTime"], CultureInfo.InvariantCulture);
                    var prod = Convert.ToInt32(row["userProductivity"], CultureInfo.InvariantCulture);

                    // first element
                    if (prodList.Count == 0)
                    {
                        var workDayStartTime = Database.GetInstance().GetUserWorkStart(date);
                        prodList.Add(new Tuple<DateTime, int>(workDayStartTime, prod));
                    }

                    // only show if it's from today
                    if (time.Date == date.Date)
                    {
                        prodList.Add(new Tuple<DateTime, int>(time, prod));
                    }
                }
                table.Dispose();
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return prodList;
        }

        /// <summary>
        /// Get a list of the times a user spent in the top (maxNumberOfPrograms) often used programs.
        /// </summary>
        /// <param name="date"></param>
        /// <param name="maxNumberOfPrograms"></param>
        /// <returns></returns>
        internal static Dictionary<string, List<TopProgramTimeDto>> GetTopProgramsUsedWithTimes(DateTimeOffset date, VisType type, int maxNumberOfPrograms)
        {
            var dto = new Dictionary<string, List<TopProgramTimeDto>>();

            try
            {
                var topProgramsUsed = GetTopProgramsUsed(date, type, maxNumberOfPrograms);

                foreach (var process in topProgramsUsed)
                {
                    var times = GetTimesForProgram(date, type, process);
                    dto.Add(process, times);
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return dto;
        }

        /// <summary>
        /// get a list of all programs, the user was active for at least 10 minutes.
        /// Only get maxNumberOfPrograms.
        /// </summary>
        /// <param name="date"></param>
        /// <param name="maxNumberOfPrograms"></param>
        /// <returns></returns>
        private static List<string> GetTopProgramsUsed(DateTimeOffset date, VisType type, int maxNumberOfPrograms)
        {
            var list = new List<string>();

            try
            {
                var query = "SELECT process "
                            + "FROM ( "
                            + "SELECT process, sum(difference) / 60.0  as 'durInMins' "
                            + "FROM (	"
                            + "SELECT t1.process, (strftime('%s', t2.time) - strftime('%s', t1.time)) as 'difference' "
                            + "FROM " + Shared.Settings.WindowsActivityTable + " t1 LEFT JOIN " + Shared.Settings.WindowsActivityTable + " t2 on t1.id + 1 = t2.id "
                            + "WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(type, date, "t1.time") + " and " + Database.GetInstance().GetDateFilteringStringForQuery(type, date, "t2.time") + " "
                            + "GROUP BY t1.id, t1.time "
                            + ") "
                            + "WHERE difference > 0 and process <> '" + Dict.Idle + "' "
                            + "GROUP BY process "
                            + "ORDER BY durInMins DESC "
                            + ") "
                            + "WHERE durInMins > 2 " // hint; assumption!
                            + "LIMIT " + maxNumberOfPrograms + ";";

                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    var process = (string)row["process"];
                    list.Add(process);
                }
                table.Dispose();
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return list;
        }

        /// <summary>
        /// For a given process and date, list all times a user was active
        /// </summary>
        /// <param name="date"></param>
        /// <param name="process"></param>
        /// <returns></returns>
        private static List<TopProgramTimeDto> GetTimesForProgram(DateTimeOffset date, VisType type, string process)
        {
            var list = new List<TopProgramTimeDto>();

            try
            {
                var dayFilter = (type == VisType.Day) ? "" : "(STRFTIME('%s', DATE(t1.time)) = STRFTIME('%s', DATE(t2.time))) and "; // needed for week view
                var query = "SELECT (strftime('%s', t2.time) - strftime('%s', t1.time)) as 'difference', t1.time as 'from', t2.time as 'to' "
                            + "FROM " + Shared.Settings.WindowsActivityTable + " t1 LEFT JOIN " + Shared.Settings.WindowsActivityTable + " t2 on t1.id + 1 = t2.id "
                            + "WHERE " + dayFilter
                            + Database.GetInstance().GetDateFilteringStringForQuery(type, date, "t1.time") + " and " + Database.GetInstance().GetDateFilteringStringForQuery(type, date, "t2.time") + " "
                            + "AND lower(t1.process) ='" + process.ToLower(CultureInfo.InvariantCulture) + "' "
                            + "GROUP BY t1.id, t1.time; ";

                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    var diff = Convert.ToInt32(row["difference"], CultureInfo.InvariantCulture);
                    var durInMins = (int)Math.Round(diff / 60.0, 0);
                    var to = DateTime.Parse((string)row["to"], CultureInfo.InvariantCulture);
                    var from = DateTime.Parse((string)row["from"], CultureInfo.InvariantCulture);

                    if (durInMins == 0) continue;
                    list.Add(new TopProgramTimeDto(from, to, durInMins));
                }
                table.Dispose();
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return list;
        }
    }

    internal class TopProgramTimeDto
    {
        public DateTime From { get; private set; }
        public DateTime To { get; private set; }
        public int DurInMins { get; private set; }

        public TopProgramTimeDto(DateTime from, DateTime to, int durInMins)
        {
            From = from;
            To = to;
            DurInMins = durInMins;
        }
    }
}
