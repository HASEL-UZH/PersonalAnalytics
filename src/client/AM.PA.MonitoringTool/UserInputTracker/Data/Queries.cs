// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using Shared.Data;
using System;
using System.Collections.Generic;
using UserInputTracker.Models;

namespace UserInputTracker.Data {
    public class Queries 
    {
        internal static void CreateUserInputTables() 
        {
            try 
            {
                Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.DbTableKeyboard + " (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, keystrokeType TEXT)");
                Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.DbTableMouseClick + " (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, x INTEGER, y INTEGER, button TEXT)");
                Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.DbTableMouseScrolling + " (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, x INTEGER, y INTEGER, scrollDelta INTEGER)");
                Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.DbTableMouseMovement + " (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, x INTEGER, y INTEGER, movedDistance INTEGER)");
            } 
            catch (Exception e) 
            {
                Shared.Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Save the keystrokes to the database. If there are more than 500 entries, 
        /// it is saved with multiple queries.
        /// </summary>
        /// <param name="keystrokes"></param>
        internal static void SaveKeystrokesToDatabase(IReadOnlyList<IUserInput> keystrokes) 
        {
            try {
                if (keystrokes == null || keystrokes.Count == 0) return;

                var newQuery = true;
                var query = "";
                int i;
                for (i = 0; i < keystrokes.Count; i++) {
                    var item = (KeystrokeEvent)keystrokes[i];
                    if (item == null) continue;

                    if (newQuery) {
                        query = "INSERT INTO '" + Settings.DbTableKeyboard + "' (time, timestamp, keystrokeType) ";
                        newQuery = false;
                    } else {
                        query += "UNION ALL ";
                    }

                    query += "SELECT strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'), " +
                                Database.GetInstance().QTime(item.Timestamp) + ", " +
                                Database.GetInstance().Q((item).KeystrokeType.ToString()) + " ";

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
                Shared.Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Save the mouse scrolls to the database. If there are more than 500 entries, 
        /// it is saved with multiple queries. This may regularly happen here
        /// </summary>
        /// <param name="mouseScrolls"></param>
        internal static void SaveMouseScrollsToDatabase(IReadOnlyList<IUserInput> mouseScrolls) 
        {
            try 
            {
                if (mouseScrolls == null || mouseScrolls.Count == 0) return;

                var newQuery = true;
                var query = "";
                int i;
                for (i = 0; i < mouseScrolls.Count; i++) {
                    var item = (MouseScrollSnapshot)mouseScrolls[i];
                    if (item == null || item.ScrollDelta == 0) continue;

                    if (newQuery) {
                        query = "INSERT INTO '" + Settings.DbTableMouseScrolling + "' (time, timestamp, x, y, scrollDelta) ";
                        newQuery = false;
                    } else {
                        query += "UNION ALL ";
                    }

                    query += "SELECT strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'), " +
                                Database.GetInstance().QTime(item.Timestamp) + ", " +
                                       Database.GetInstance().Q(item.X.ToString()) + ", " +
                                       Database.GetInstance().Q(item.Y.ToString()) + ", " +
                                       Database.GetInstance().Q(item.ScrollDelta.ToString()) + " ";

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
                Shared.Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Save the mouse clicks to the database. If there are more than 500 entries, 
        /// it is saved with multiple queries.
        /// </summary>
        /// <param name="mouseClicks"></param>
        internal static void SaveMouseClicksToDatabase(IReadOnlyList<IUserInput> mouseClicks) 
        {
            try 
            {
                if (mouseClicks == null || mouseClicks.Count == 0) return;

                var newQuery = true;
                var query = "";
                int i;
                for (i = 0; i < mouseClicks.Count; i++) {
                    var item = (MouseClickEvent)mouseClicks[i];
                    if (item == null) continue;

                    if (newQuery) {
                        query = "INSERT INTO '" + Settings.DbTableMouseClick + "' (time, timestamp, x, y, button) ";
                        newQuery = false;
                    } else {
                        query += "UNION ALL ";
                    }

                    query += "SELECT strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'), " +
                                Database.GetInstance().QTime(item.Timestamp) + ", " +
                                Database.GetInstance().Q(item.X.ToString()) + ", " +
                                Database.GetInstance().Q(item.Y.ToString()) + ", " +
                                Database.GetInstance().Q(item.Button.ToString()) + " ";

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
                Shared.Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Save the mouse movements to the database. If there are more than 500 entries, 
        /// it is saved with multiple queries.
        /// </summary>
        /// <param name="mouseMovements"></param>
        internal static void SaveMouseMovementsToDatabase(IReadOnlyList<IUserInput> mouseMovements) 
        {
            try 
            {
                if (mouseMovements == null || mouseMovements.Count == 0) return;

                var newQuery = true;
                var query = "";
                int i;
                for (i = 0; i < mouseMovements.Count; i++) {
                    var item = (MouseMovementSnapshot)mouseMovements[i];
                    if (item == null || item.MovedDistance == 0) continue;

                    if (newQuery) {
                        query = "INSERT INTO '" + Settings.DbTableMouseMovement + "' (time, timestamp, x, y, movedDistance) ";
                        newQuery = false;
                    } else {
                        query += "UNION ALL ";
                    }

                    query += "SELECT strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'), " +
                                Database.GetInstance().QTime(item.Timestamp) + ", " +
                                Database.GetInstance().Q(item.X.ToString()) + ", " +
                                Database.GetInstance().Q(item.Y.ToString()) + ", " +
                                Database.GetInstance().Q(item.MovedDistance.ToString()) + " ";

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
                Shared.Logger.WriteToLogFile(e);
            }
        }
    }
}
