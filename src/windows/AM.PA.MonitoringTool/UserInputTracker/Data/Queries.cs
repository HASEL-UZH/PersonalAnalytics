// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using Shared;
using Shared.Data;
using Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using UserInputTracker.Models;

namespace UserInputTracker.Data
{
    public class Queries 
    {
        internal static void CreateUserInputTables() 
        {
            try 
            {
                // V2.0: only one table needed as we store an aggregate
                Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.DbTableUserInput_v2 + " (id INTEGER PRIMARY KEY, time TEXT, tsStart TEXT, tsEnd TEXT, keyTotal INTEGER, keyOther INTEGER, keyBackspace INTEGER, keyNavigate INTEGER, clickTotal INTEGER, clickOther INTEGER, clickLeft INTEGER, clickRight INTEGER, scrollDelta INTEGER, movedDistance INTEGER)");
                
                // V1.0: detailed (old) tables (used just for studies)
                if (Settings.IsDetailedCollectionEnabled)
                {
                    Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.DbTableKeyboard_v1 + " (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, keystrokeType TEXT)");
                    Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.DbTableMouseClick_v1 + " (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, x INTEGER, y INTEGER, button TEXT)");
                    Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.DbTableMouseScrolling_v1 + " (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, x INTEGER, y INTEGER, scrollDelta INTEGER)");
                    Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.DbTableMouseMovement_v1 + " (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, x INTEGER, y INTEGER, movedDistance INTEGER)");
                }
            } 
            catch (Exception e) 
            {
                Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Saves UserInputAggregate to the database
        /// (V2.0, in the old version (V1.0) each entry was stored in a corresponding table)
        /// </summary>
        /// <param name="ma"></param>
        internal static void SaveUserInputSnapshotToDatabase(UserInputAggregate ma)
        {
            var sb = new StringBuilder();
            sb.Append("INSERT INTO '");
            sb.Append(Settings.DbTableUserInput_v2);
            sb.Append("' (time, tsStart, tsEnd, keyTotal, keyOther, keyBackspace, keyNavigate, clickTotal, clickOther, clickLeft, clickRight, scrollDelta, movedDistance) VALUES (");

            sb.Append("strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'), ");
            sb.Append(Database.GetInstance().QTime2(ma.TsStart)); sb.Append(",");
            sb.Append(Database.GetInstance().QTime2(ma.TsEnd)); sb.Append(",");

            sb.Append(Database.GetInstance().Q(ma.KeyTotal)); sb.Append(",");
            sb.Append(Database.GetInstance().Q(ma.KeyOther)); sb.Append(",");
            sb.Append(Database.GetInstance().Q(ma.KeyBackspace)); sb.Append(",");
            sb.Append(Database.GetInstance().Q(ma.KeyNavigate)); sb.Append(",");

            sb.Append(Database.GetInstance().Q(ma.ClickTotal)); sb.Append(",");
            sb.Append(Database.GetInstance().Q(ma.ClickOther)); sb.Append(",");
            sb.Append(Database.GetInstance().Q(ma.ClickLeft)); sb.Append(",");
            sb.Append(Database.GetInstance().Q(ma.ClickRight)); sb.Append(",");

            sb.Append(Database.GetInstance().Q(ma.ScrollDelta)); sb.Append(",");

            sb.Append(Database.GetInstance().Q(ma.MovedDistance));

            sb.Append(");");

            var query = sb.ToString();
            Database.GetInstance().ExecuteDefaultQuery(query);
        }

        #region Save detailed user input tracking into the database (V1.0, old)

        /// <summary>
        /// Save the keystroke type (not exact keystroke) to the database. If there are more than 500 entries, 
        /// it is saved with multiple queries.
        /// </summary>
        /// <param name="keystrokes"></param>
        internal static void SaveKeystrokesToDatabase(List<KeystrokeEvent> keystrokes)
        {
            try {
                if (keystrokes == null || keystrokes.Count == 0) return;

                var newQuery = true;
                var query = "";
                int i;
                for (i = 0; i<keystrokes.Count; i++) {
                    var item = keystrokes[i];
                    if (item == null) continue;

                    if (newQuery) {
                        query = "INSERT INTO '" + Settings.DbTableKeyboard_v1 + "' (time, timestamp, keystrokeType) ";
                        newQuery = false;
                    } else {
                        query += "UNION ALL ";
                    }

                    query += "SELECT strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'), " +
                                Database.GetInstance().QTime2(item.Timestamp) + ", " +
                                Database.GetInstance().Q((item).KeystrokeType.ToString()) + " "; // keystroke-type not keystroke, avoid key-logging!

                    //executing remaining lines
                    if (i != 0 && i % 499 == 0) {
                        Database.GetInstance().ExecuteDefaultQuery(query);
                        newQuery = true;
                        query = string.Empty;
                    }
                }

                //executing remaining lines
                if (i % 499 != 0) {
                    Database.GetInstance().ExecuteDefaultQuery(query);
                }
            } 
            catch (Exception e) 
            {
                Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Save the mouse scrolls to the database. If there are more than 500 entries, 
        /// it is saved with multiple queries. This may regularly happen here
        /// </summary>
        /// <param name="mouseScrolls"></param>
        internal static void SaveMouseScrollsToDatabase(List<MouseScrollSnapshot> mouseScrolls)
        {
            try
            {
                if (mouseScrolls == null || mouseScrolls.Count == 0) return;

                var newQuery = true;
                var query = "";
                int i;
                for (i = 0; i < mouseScrolls.Count; i++)
                {
                    var item = mouseScrolls[i];
                    if (item == null || item.ScrollDelta == 0) continue;

                    if (newQuery)
                    {
                        query = "INSERT INTO '" + Settings.DbTableMouseScrolling_v1 + "' (time, timestamp, x, y, scrollDelta) ";
                        newQuery = false;
                    }
                    else
                    {
                        query += "UNION ALL ";
                    }

                    query += "SELECT strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'), " +
                                Database.GetInstance().QTime2(item.Timestamp) + ", " +
                                       Database.GetInstance().Q(item.X) + ", " +
                                       Database.GetInstance().Q(item.Y) + ", " +
                                       Database.GetInstance().Q(Math.Abs(item.ScrollDelta)) + " ";

                    //executing remaining lines
                    if (i != 0 && i % 499 == 0)
                    {
                        Database.GetInstance().ExecuteDefaultQuery(query);
                        newQuery = true;
                        query = string.Empty;
                    }
                }

                //executing remaining lines
                if (i % 499 != 0)
                {
                    Database.GetInstance().ExecuteDefaultQuery(query);
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Save the mouse clicks to the database. If there are more than 500 entries, 
        /// it is saved with multiple queries.
        /// </summary>
        /// <param name="mouseClicks"></param>
        internal static void SaveMouseClicksToDatabase(List<MouseClickEvent> mouseClicks)
        {
            try
            {
                if (mouseClicks == null || mouseClicks.Count == 0) return;

                var newQuery = true;
                var query = "";
                int i;
                for (i = 0; i < mouseClicks.Count; i++)
                {
                    var item = mouseClicks[i];
                    if (item == null) continue;

                    if (newQuery)
                    {
                        query = "INSERT INTO '" + Settings.DbTableMouseClick_v1 + "' (time, timestamp, x, y, button) ";
                        newQuery = false;
                    }
                    else
                    {
                        query += "UNION ALL ";
                    }

                    query += "SELECT strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'), " +
                                Database.GetInstance().QTime2(item.Timestamp) + ", " +
                                Database.GetInstance().Q(item.X) + ", " +
                                Database.GetInstance().Q(item.Y) + ", " +
                                Database.GetInstance().Q(item.Button.ToString()) + " ";

                    //executing remaining lines
                    if (i != 0 && i % 499 == 0)
                    {
                        Database.GetInstance().ExecuteDefaultQuery(query);
                        newQuery = true;
                        query = string.Empty;
                    }
                }

                //executing remaining lines
                if (i % 499 != 0)
                {
                   Database.GetInstance().ExecuteDefaultQuery(query);
                }
            }
            catch (Exception e)
            {
                Shared.Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Save the mouse movements to the database. If there are more than 500 entries, 
        /// it is saved with multiple queries.
        /// </summary>
        /// <param name="mouseMovements"></param>
        internal static void SaveMouseMovementsToDatabase(List<MouseMovementSnapshot> mouseMovements)
        {
            try
            {
                if (mouseMovements == null || mouseMovements.Count == 0) return;

                var newQuery = true;
                var query = "";
                int i;
                for (i = 0; i < mouseMovements.Count; i++)
                {
                    var item = mouseMovements[i];
                    if (item == null || item.MovedDistance == 0) continue;

                    if (newQuery)
                    {
                        query = "INSERT INTO '" + Settings.DbTableMouseMovement_v1 + "' (time, timestamp, x, y, movedDistance) ";
                        newQuery = false;
                    }
                    else
                    {
                        query += "UNION ALL ";
                    }

                    query += "SELECT strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'), " +
                                Database.GetInstance().QTime2(item.Timestamp) + ", " +
                                Database.GetInstance().Q(item.X) + ", " +
                                Database.GetInstance().Q(item.Y) + ", " +
                                Database.GetInstance().Q(item.MovedDistance) + " ";

                    //executing remaining lines
                    if (i != 0 && i % 499 == 0)
                    {
                        Database.GetInstance().ExecuteDefaultQuery(query);
                        newQuery = true;
                        query = string.Empty;
                    }
                }

                //executing remaining lines
                if (i % 499 != 0)
                {
                    Database.GetInstance().ExecuteDefaultQuery(query);
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        #endregion

        /// <summary>
        /// Returns a dictionary with an input-level like data set for each interval (Settings.UserInputVisMinutesInterval)
        /// (new version, 2.0)
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal static Dictionary<DateTime, int> GetUserInputTimelineData_v2(DateTimeOffset date)
        {
            var dto = new Dictionary<DateTime, int>();

            // prepare Dictionary
            VisHelper.PrepareTimeAxis(date, dto, Settings.UserInputVisMinutesInterval);

            // get user input data (aggregate)
            try
            {
                var query = "SELECT tsStart, tsEnd, keyTotal, clickTotal, scrollDelta, movedDistance FROM " + Settings.DbTableUserInput_v2 + " WHERE STRFTIME('%s', DATE(time))==STRFTIME('%s', DATE('" + date.Date.ToString("u", CultureInfo.InvariantCulture) + "'));";
                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    try
                    {
                        var time = DateTime.Parse((string)row["tsStart"], CultureInfo.InvariantCulture);
                        time = time.AddSeconds(-time.Second); // nice seconds

                        // find 10 minutes interval
                        time = DateTimeHelper.RoundUp(time, TimeSpan.FromMinutes(Settings.UserInputVisMinutesInterval));

                        // add values
                        if (dto.ContainsKey(time))
                        {
                            var keystrokes = (long)row["keyTotal"];
                            var mouseClicks = (long)row["clickTotal"];
                            var mouseScrollDistance = (long)row["scrollDelta"];
                            var mouseMoves = (long)row["movedDistance"];

                            dto[time] += CalculateUserInputLevel(keystrokes, mouseClicks, mouseScrollDistance, mouseMoves);
                        }
                    }
                    catch { }
                }
                table.Dispose();
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return dto;
        }

        public static int CalculateUserInputLevel(long keystrokes, long mouseClicks, double mouseMoves, double mouseScrollDistance)
        {
            return (int)keystrokes +
                   (int)(Settings.MouseClickKeyboardRatio * mouseClicks) +
                   (int)Math.Round((Settings.MouseMovementKeyboardRatio * mouseMoves)) +
                   (int)Math.Round((Settings.MouseScrollingKeyboardRatio * mouseScrollDistance));
        }

        /// <summary>
        /// Returns a dictionary with an input-level like data set for each interval (Settings.UserInputVisMinutesInterval)
        /// (old version, 1.0)
        /// 
        /// TODO: delete/deprecate at some point
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        /*internal static Dictionary<DateTime, int> GetUserInputTimelineData_v1(DateTimeOffset date)
        {
            var dto = new Dictionary<DateTime, int>();

            try
            {
                // 1. prepare Dictionary
                VisHelper.PrepareTimeAxis(date, dto, Settings.UserInputVisMinutesInterval);

                // 2. fill keyboard data
                try
                {
                    var queryKeystrokes = "SELECT timestamp FROM " + Settings.DbTableKeyboard_v1 + " WHERE STRFTIME('%s', DATE(time))==STRFTIME('%s', DATE('" + date.Date.ToString("u", CultureInfo.InvariantCulture) + "'));";
                    var tableKeystrokes = Database.GetInstance().ExecuteReadQuery(queryKeystrokes);

                    foreach (DataRow row in tableKeystrokes.Rows)
                    {
                        var time = DateTime.Parse((string)row["timestamp"], CultureInfo.InvariantCulture);
                        time = time.AddSeconds(-time.Second); // nice seconds

                        // find 10 minutes interval
                        time = Shared.Helpers.DateTimeHelper.RoundUp(time, TimeSpan.FromMinutes(Settings.UserInputVisMinutesInterval));

                        // add keystroke
                        if (dto.ContainsKey(time)) dto[time]++;
                    }
                    tableKeystrokes.Dispose();
                }
                catch (Exception e)
                {
                    Logger.WriteToLogFile(e);
                }

                // 3. fill mouse click data
                try
                {
                    var queryMouseClicks = "SELECT timestamp FROM " + Settings.DbTableMouseClick_v1 + " WHERE STRFTIME('%s', DATE(time))==STRFTIME('%s', DATE('" + date.Date.ToString("u", CultureInfo.InvariantCulture) + "'));";
                    var tableMouseClicks = Database.GetInstance().ExecuteReadQuery(queryMouseClicks);
                    foreach (DataRow row in tableMouseClicks.Rows)
                    {
                        var time = DateTime.Parse((string)row["timestamp"], CultureInfo.InvariantCulture);
                        time = time.AddSeconds(-time.Second); // nice seconds

                        // find 10 minutes interval
                        time = Shared.Helpers.DateTimeHelper.RoundUp(time, TimeSpan.FromMinutes(Settings.UserInputVisMinutesInterval));

                        // add mouse click (with weighting)
                        if (dto.ContainsKey(time)) dto[time] += Settings.MouseClickKeyboardRatio;
                    }
                    tableMouseClicks.Dispose();
                }
                catch (Exception e)
                {
                    Logger.WriteToLogFile(e);
                }

                // 4. fill mouse scrolling data
                try
                {
                    var queryMouseScrolls = "SELECT timestamp, scrollDelta FROM " + Settings.DbTableMouseScrolling_v1 + " WHERE STRFTIME('%s', DATE(time))==STRFTIME('%s', DATE('" + date.Date.ToString("u", CultureInfo.InvariantCulture) + "')) AND scrollDelta > 0;";
                    var tableMouseScrolls = Database.GetInstance().ExecuteReadQuery(queryMouseScrolls);
                    foreach (DataRow row in tableMouseScrolls.Rows)
                    {
                        var time = DateTime.Parse((string)row["timestamp"], CultureInfo.InvariantCulture);
                        time = time.AddSeconds(-time.Second); // nice seconds

                        // find 10 minutes interval
                        time = Shared.Helpers.DateTimeHelper.RoundUp(time, TimeSpan.FromMinutes(Settings.UserInputVisMinutesInterval));

                        // add mouse scrolling (with weighting)
                        var scroll = (long)row["scrollDelta"];
                        if (scroll > 0 && dto.ContainsKey(time)) dto[time] += (int)Math.Round(scroll * Settings.MouseScrollingKeyboardRatio, 0);
                    }
                    tableMouseScrolls.Dispose();
                }
                catch (Exception e)
                {
                    Logger.WriteToLogFile(e);
                }

                // 5. fill mouse move data
                try
                {
                    var queryMouseMovements = "SELECT timestamp, movedDistance FROM " + Settings.DbTableMouseMovement_v1 + " WHERE STRFTIME('%s', DATE(time))==STRFTIME('%s', DATE('" + date.Date.ToString("u", CultureInfo.InvariantCulture) + "')) AND movedDistance > 0;";
                    var tableMouseMovements = Database.GetInstance().ExecuteReadQuery(queryMouseMovements);
                    foreach (DataRow row in tableMouseMovements.Rows)
                    {
                        var time = DateTime.Parse((string)row["timestamp"], CultureInfo.InvariantCulture);
                        time = time.AddSeconds(-time.Second); // nice seconds

                        // find 10 minutes interval
                        time = Shared.Helpers.DateTimeHelper.RoundUp(time, TimeSpan.FromMinutes(Settings.UserInputVisMinutesInterval));

                        // add mouse movement (with weighting)
                        var moved = (long)row["movedDistance"];
                        if (moved > 0 && dto.ContainsKey(time)) dto[time] += (int)Math.Round(moved * Settings.MouseMovementKeyboardRatio, 0);
                    }
                    tableMouseMovements.Dispose();
                }
                catch (Exception e)
                {
                    Logger.WriteToLogFile(e);
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return dto;
        }*/
    }
}
