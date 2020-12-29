// Created by André Meyer at MSR
// Created: 2015-11-12
// 
// Licensed under the MIT License.

using MsOfficeTracker.Helpers;
using Shared;
using Shared.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace MsOfficeTracker.Data
{
    public class Queries
    {
        internal static void CreateMsTrackerTables()
        {
            try
            {
                Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.MeetingsTable + " (id INTEGER PRIMARY KEY, timestamp TEXT, time TEXT, subject TEXT, durationInMins INTEGER, numAttendees INTEGER);");
                Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.EmailsTable + " (id INTEGER PRIMARY KEY, timestamp TEXT, time TEXT, inbox INTEGER, inboxUnread INTEGER, sent INTEGER, received INTEGER, receivedUnread INTEGER, isFromTimer INTEGER);");
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
                // database update 07.12.2017 (added one column to 'meetings' table)
                if (version == 5)
                {
                    if (Database.GetInstance().HasTable(Settings.MeetingsTable))
                    {
                        Database.GetInstance().ExecuteDefaultQuery("ALTER TABLE " + Settings.MeetingsTable + " ADD COLUMN numAttendees INTEGER;");
                    }
                }

                // database update 20.06.2016 (added two columns to 'emails' table)
                if (version == 2)
                {
                    if (Database.GetInstance().HasTable(Settings.EmailsTable))
                    {
                        Database.GetInstance().ExecuteDefaultQuery("ALTER TABLE " + Settings.EmailsTable + " ADD COLUMN inboxUnread INTEGER;");
                        Database.GetInstance().ExecuteDefaultQuery("ALTER TABLE " + Settings.EmailsTable + " ADD COLUMN receivedUnread INTEGER;");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Class calls the API to determine the inbox size, number of emails sent and received.
        /// (inbox size: is only collected for date = DateTime.Now)
        /// </summary>
        /// <param name="date"></param>
        /// <param name="isFromTimer"></param>
        internal static Tuple<long, long, long, long, int> CreateEmailsSnapshot(DateTime date, bool isFromTimer)
        {
            try
            {
                // get inbox size (can only be done today)
                var unreadInbox = Settings.NoValueDefault;
                var inbox = Settings.NoValueDefault;
                var unreadReceived = Settings.NoValueDefault;
                if (date.Date == DateTime.Now.Date)
                {
                    // unread inbox size
                    var unreadInboxSizeResponse = Office365Api.GetInstance().GetNumberOfUnreadEmailsInInbox();
                    unreadInboxSizeResponse.Wait();
                    unreadInbox = (int)unreadInboxSizeResponse.Result;

                    // total inbox size
                    var inboxSizeResponse = Office365Api.GetInstance().GetTotalNumberOfEmailsInInbox();
                    inboxSizeResponse.Wait();
                    inbox = (int)inboxSizeResponse.Result;

                    // get unread emails received count
                    var unreadReceivedResult = Office365Api.GetInstance().GetNumberOfUnreadEmailsReceived(date.Date);
                    unreadReceivedResult.Wait();
                    unreadReceived = unreadReceivedResult.Result;
                }

                // get emails sent count
                var sentResult = Office365Api.GetInstance().GetNumberOfEmailsSent(date.Date);
                sentResult.Wait();
                var sent = sentResult.Result;

                // get total emails received count
                var receivedResult = Office365Api.GetInstance().GetTotalNumberOfEmailsReceived(date.Date);
                receivedResult.Wait();
                var received = receivedResult.Result;

                // save into the database
                SaveEmailsSnapshot(date, inbox, unreadInbox, sent, received, unreadReceived, isFromTimer);

                // return for immediate use
                return new Tuple<long, long, long, long, int>(inbox, unreadInbox, sent, received, unreadReceived);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return new Tuple<long, long, long, long, int>(Settings.NoValueDefault, Settings.NoValueDefault, Settings.NoValueDefault, Settings.NoValueDefault, Settings.NoValueDefault);
            }
        }

        /// <summary>
        /// Saves the timestamp, inbox size, sent items count, received items count into the database
        /// </summary>
        /// <param name="date"></param>
        /// <param name="inbox"></param>
        /// <param name="unreadInbox"></param>
        /// <param name="sent"></param>
        /// <param name="received"></param>
        /// <param name="unreadReceived"></param>
        /// <param name="isFromTimer"></param>
        internal static void SaveEmailsSnapshot(DateTime date, long inbox, long unreadInbox, long sent, long received, int unreadReceived, bool isFromTimer)
        {
            Database.GetInstance().ExecuteDefaultQuery("INSERT INTO " + Settings.EmailsTable + " (timestamp, time, inbox, inboxUnread, sent, received, receivedUnread, isFromTimer) VALUES (strftime('%Y-%m-%d %H:%M:%f', 'now', 'localtime'), " +
                Database.GetInstance().QTime(date) + ", " + Database.GetInstance().Q(inbox) + ", " + Database.GetInstance().Q(unreadInbox) + ", " + Database.GetInstance().Q(sent) + ", "  +
                Database.GetInstance().Q(received) + ", " + Database.GetInstance().Q(unreadReceived) + ", " + Database.GetInstance().Q(isFromTimer) + ");");
        }

        /// <summary>
        /// Saves the timestamp, subject and duration in minutes to the database
        /// only if the meeting is not yet stored for a given day
        /// 
        /// Hint: The meeting subject can be obfuscated by setting the property RecordMeetingTitles to false
        /// </summary>
        /// <param name="date"></param>
        /// <param name="subject"></param>
        /// <param name="durationInMins"></param>
        /// <param name="numberOfAttendees"></param>
        internal static void SaveMeetingsSnapshot(DateTime date, string subject, int durationInMins, int numberOfAttendees)
        {
            if (Shared.Settings.AnonymizeSensitiveData)
            {
                subject = Dict.Anonymized;  // obfuscate window title
            }

            var query = "INSERT INTO " + Settings.MeetingsTable + " (timestamp, time, subject, durationInMins, numAttendees) "
                        + "SELECT strftime('%Y-%m-%d %H:%M:%f', 'now', 'localtime'), " + 
                        //Database.GetInstance().Q(id) + ", " + // could also save ID to validate duplicates below with it (but needs quite some space)
                        Database.GetInstance().QTime(date) + ", " + 
                        Database.GetInstance().Q(subject) + ", " + 
                        Database.GetInstance().Q(durationInMins) + ", " + 
                        Database.GetInstance().Q(numberOfAttendees) + " "
                        // check if a duplicate entry exists (subject and date) - changes in number of attendees and duration not considered
                        + "WHERE NOT EXISTS ("
                            + "SELECT 1 FROM " + Settings.MeetingsTable + " WHERE time = " + Database.GetInstance().QTime(date) + " AND subject = " + Database.GetInstance().Q(subject) 
                        + ");";

            Database.GetInstance().ExecuteDefaultQuery(query);
        }

        /// <summary>
        /// Check if there is already meetings stored for the date
        /// </summary>
        /// <returns>true if yes, false otherwise</returns>
        internal static bool HasMeetingEntriesForDate(DateTimeOffset date)
        {
            try
            {
                var query = "SELECT EXISTS( "
                          + "SELECT 1 FROM " + Settings.MeetingsTable + " "
                          + "WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date) + "); ";

                var count = Database.GetInstance().ExecuteScalar(query);
                return count != 0;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return false;
            }
        }

        /// <summary>
        /// Removes all stored meetings for date so they can be updated
        /// (necessary in case someone deletes a meeting)
        /// </summary>
        /// <param name="date"></param>
        internal static void RemoveMeetingsForDate(DateTimeOffset date)
        {
            var query = "DELETE FROM " + Settings.MeetingsTable + " WHERE time = " + Database.GetInstance().QTime(date.Date) + ";";
            Database.GetInstance().ExecuteDefaultQuery(query);
        }

        /// <summary>
        /// Check if there is already emails (with isFromTimer = 0) stored for the date
        /// </summary>
        /// <returns>true if yes, false otherwise</returns>
        internal static bool HasEmailsEntriesForDate(DateTimeOffset date, bool isFromTimer)
        {
            try
            {
                var query = "SELECT EXISTS( "
                          + "SELECT 1 FROM " + Settings.EmailsTable + " "
                          + "WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date) + " "
                          + "AND isFromTimer = " + Database.GetInstance().Q(isFromTimer) + ");";

                var count = Database.GetInstance().ExecuteScalar(query);
                if (count == 0) return false;
                return true;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return false;
            }
        }

        /// <summary>
        /// email metrics
        /// </summary>
        /// <param name="date"></param>
        internal static Tuple<DateTime, long, long, long, long, long> GetEmailsSnapshot(DateTimeOffset date)
        {
            try
            {
                var query = "SELECT time, inbox, inboxUnread, sent, received, receivedUnread FROM " + Settings.EmailsTable + " "
                            + "WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date) + " "
                            + "ORDER BY time DESC "
                            + "LIMIT 1;";

                var table = Database.GetInstance().ExecuteReadQuery(query);

                if (table != null && table.Rows.Count == 1)
                {
                    var row = table.Rows[0];

                    var inbox = row.IsNull("inbox") ? Settings.NoValueDefault : Convert.ToInt64(row["inbox"], CultureInfo.InvariantCulture);
                    var inboxUnread = row.IsNull("inboxUnread") ? Settings.NoValueDefault : Convert.ToInt64(row["inboxUnread"], CultureInfo.InvariantCulture);
                    var sent = row.IsNull("sent") ? Settings.NoValueDefault : Convert.ToInt64(row["sent"], CultureInfo.InvariantCulture);
                    var received = row.IsNull("received") ? Settings.NoValueDefault : Convert.ToInt64(row["received"], CultureInfo.InvariantCulture);
                    var receivedUnread = row.IsNull("receivedUnread") ? Settings.NoValueDefault : Convert.ToInt64(row["receivedUnread"], CultureInfo.InvariantCulture);
                    var timestamp = DateTime.Parse((string)row["time"], CultureInfo.InvariantCulture);

                    table.Dispose();
                    return new Tuple<DateTime, long, long, long, long, long>(timestamp, inbox, inboxUnread, sent, received, receivedUnread);
                }
                else
                {
                    table?.Dispose();
                    return new Tuple<DateTime, long, long, long, long, long>(DateTime.MinValue, Settings.NoValueDefault, Settings.NoValueDefault, Settings.NoValueDefault, Settings.NoValueDefault, Settings.NoValueDefault);
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return new Tuple<DateTime, long, long, long, long, long>(DateTime.MinValue, Settings.NoValueDefault, Settings.NoValueDefault, Settings.NoValueDefault, Settings.NoValueDefault, Settings.NoValueDefault);
            }
        }

        /// <summary>
        /// Averages over 'numberOfDays' for the email metrics
        /// </summary>
        /// <param name="numberOfDays">how many days back for average</param>
        internal static Tuple<double, double, double, double, double> GetEmailsSnapshotAverages(int numberOfDays)
        {
            try
            {
                var query =   @"SELECT DATE(e.time), e.inbox, e.inboxUnread, e.sent, e.received, e.receivedUnread
                                  FROM " + Settings.EmailsTable + @"  as e
                                  JOIN (SELECT MAX(ee.time) 'maxtimestamp'
                                         FROM emails ee
                                     GROUP BY date(ee.time)) as m ON m.maxtimestamp = e.time
                                ORDER BY time DESC
                                LIMIT " + numberOfDays + ";";

                var table = Database.GetInstance().ExecuteReadQuery(query);


                var inboxItems = new List<long>();
                var inboxUnreadItems = new List<long>();
                var sentItems = new List<long>();
                var receivedItems = new List<long>();
                var receivedUnreadItems = new List<long>();

                foreach (DataRow row in table.Rows)
                {
                    inboxItems.Add(row.IsNull("inbox") ? Settings.NoValueDefault : Convert.ToInt64(row["inbox"], CultureInfo.InvariantCulture));
                    inboxUnreadItems.Add(row.IsNull("inboxUnread") ? Settings.NoValueDefault : Convert.ToInt64(row["inboxUnread"], CultureInfo.InvariantCulture));
                    sentItems.Add(row.IsNull("sent") ? Settings.NoValueDefault : Convert.ToInt64(row["sent"], CultureInfo.InvariantCulture));
                    receivedItems.Add(row.IsNull("received") ? Settings.NoValueDefault : Convert.ToInt64(row["received"], CultureInfo.InvariantCulture));
                    receivedUnreadItems.Add(row.IsNull("receivedUnread") ? Settings.NoValueDefault : Convert.ToInt64(row["receivedUnread"], CultureInfo.InvariantCulture));
                }
                table.Dispose();

                var inboxAvg = inboxItems.Where(e => e > Settings.NoValueDefault).DefaultIfEmpty().Average(e => e);
                var inboxUnreadAvg = inboxUnreadItems.Where(e => e > Settings.NoValueDefault).DefaultIfEmpty().Average(e => e);
                var sentAvg = sentItems.Where(e => e > Settings.NoValueDefault).DefaultIfEmpty().Average(e => e);
                var receivedAvg = receivedItems.Where(e => e > Settings.NoValueDefault).DefaultIfEmpty().Average(e => e);
                var receivedUnreadAvg = receivedUnreadItems.Where(e => e > Settings.NoValueDefault).DefaultIfEmpty().Average(e => e);

                return new Tuple<double, double, double, double, double>(inboxAvg, inboxUnreadAvg, sentAvg, receivedAvg, receivedUnreadAvg);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return new Tuple<double, double, double, double, double>(Settings.NoValueDefault, Settings.NoValueDefault, Settings.NoValueDefault, Settings.NoValueDefault, Settings.NoValueDefault);
            }
        }

        /// <summary>
        /// The emails sent and inbox size
        /// </summary>
        /// <param name="date"></param>
        /// <returns>Tuple item1: sent, item2: received</returns>
        //internal static Tuple<DateTime, int> GetAverageInboxSize(DateTimeOffset date)
        //{
        //    try
        //    {
        //        var query = "SELECT time, avg(inbox) as avg FROM " + Settings.EmailsTable + " "
        //                    + "WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date) + " "
        //                    + "AND inbox != -1 "
        //                    + "ORDER BY timestamp DESC "
        //                    + "LIMIT 1;";

        //        var table = Database.GetInstance().ExecuteReadQuery(query);

        //        if (table != null && table.Rows.Count == 1)
        //        {
        //            var row = table.Rows[0];
        //            var inbox = Convert.ToDouble(row["avg"], CultureInfo.InvariantCulture);
        //            var inboxRounded = (int)Math.Round(inbox, 0);
        //            var timestamp = DateTime.Parse((string)row["time"], CultureInfo.InvariantCulture);

        //            table.Dispose();
        //            return new Tuple<DateTime, int>(timestamp, inboxRounded);
        //        }
        //        else
        //        {
        //            table.Dispose();
        //            return new Tuple<DateTime, int>(DateTime.MinValue, -1);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        //Logger.WriteToLogFile(e);
        //        return new Tuple<DateTime, int>(DateTime.MinValue, -1);
        //    }
        //}
    }
}
