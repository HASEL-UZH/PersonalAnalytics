// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;

namespace Shared.Data
{
    enum LogType { Info, Warning, Error }

    /// <summary>
    /// This is the implementation of the database with all queries, commands, etc.
    /// </summary>
    public sealed class DatabaseImplementation : IDisposable
    {
        private SQLiteConnection _connection; // null if not connected
        public string CurrentDatabaseDumpFile; // every week a new database file
        public static readonly string DB_FORMAT_DAY_AND_TIME = "yyyy-MM-dd HH:mm:ss";

        public void Dispose()
        {
            _connection.Dispose();
        }

        #region Execute Queries & Log Messages

        /// <summary>
        /// Returns true if the table passed in the parameter exists and false otherwise.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public bool HasTable(string tableName)
        {
            if (_connection == null || _connection.State != ConnectionState.Open) throw new Exception("Connection to database not established.");
            try
            {
                var query = "SELECT name FROM sqlite_master WHERE type ='table' AND name='" + tableName + "';";

                DataTable result = ExecuteReadQuery(query);
                if (result == null || result.Rows.Count == 0)
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

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
                Logger.WriteToLogFile(new Exception("Query: " + query));
                return 0;
            } 
            finally 
            {
                cmd.Dispose();    
            }
        }

        /// <summary>
        /// Executes a query scalar. Returns the INT in case of an entry, 
        /// or 0 in case of no entry or an error.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public int ExecuteScalar(string query)
        {
            WriteQueryToLog(query);
            if (_connection == null || _connection.State != ConnectionState.Open) return 0;

            var cmd = new SQLiteCommand(query, _connection);
            try
            {
                var res = cmd.ExecuteScalar();
                return (DBNull.Value != res) ? Convert.ToInt32(res) : 0;
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
        /// Executes a query scalar and returns null if there was an error (or no entry)
        /// used for settings. Somehow strange?
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public object ExecuteScalar2(string query)
        {
            try { return new SQLiteCommand(query, _connection).ExecuteScalar(); }
            catch { return null; }
        }

        /// <summary>
        /// Executes a query scalar. Return the DOUBLE in case of an entry, 
        /// or 0.0 in case of no entry or an error.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public double ExecuteScalar3(string query)
        {
            WriteQueryToLog(query);
            if (_connection == null || _connection.State != ConnectionState.Open) return 0.0;

            var cmd = new SQLiteCommand(query, _connection);
            try
            {
                var res = cmd.ExecuteScalar();
                return (DBNull.Value != res) ? Convert.ToDouble(res) : 0.0;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return 0.0;
            }
            finally
            {
                cmd.Dispose();
            }
        }

        public void ExecuteQueryWithTransaction(List<string> commands)
        {
           //try
           // {
           //     using (var cmd = new SQLiteCommand(_connection))
           //     {
           //         using (var transaction = _connection.BeginTransaction())
           //         {
           //             foreach (var item in commands)
           //             {
           //                 cmd.CommandText = item;
           //                 cmd.ExecuteNonQuery();
           //             }
           //             transaction.Commit();
           //         }
           //     }
           // }
           // catch (Exception e)
           // {

           // }
           // finally
           // {

           // }
        }

        /// <summary>
        /// Executes a query (given as a parameter).
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
            catch
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
            var message = string.Format(CultureInfo.InvariantCulture, "An unknown exception occurred in the tracker '{0}'.", tracker);
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
        /// Q is the James-Bond guy. But for ints.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string Q(int str)
        {
            return "'" + str + "'";
        }

        /// <summary>
        /// Q is the James-Bond guy. But for longs.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string Q(long str)
        {
            return "'" + str + "'";
        }

        /// <summary>
        /// Q is the James-Bond guy. But for bools.
        /// </summary>
        /// <param name="str"></param>
        /// <returns>1 if true, 0 if false</returns>
        public string Q(bool str)
        {
            if (str == true) return "'" + 1 + "'";
            else return "'" + 0 + "'";
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
        /// Formats and magicifies a datetime with fractional seconds
        /// '%Y-%m-%d %H:%M:%f'
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public string QTime2(DateTime dateTime)
        {
            var dateTimeString = dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"); // dateTime.ToShortDateString() + " " + dateTime.ToLongTimeString();
            return Q(dateTimeString);
        }

        /// <summary>
        /// Formats and magicifies a datetime
        /// '%Y-%m-%d'
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public string QDate(DateTime dateTime)
        {
            var dateTimeString = dateTime.ToString("yyyy-MM-dd"); // dateTime.ToShortDateString() + " " + dateTime.ToLongTimeString();
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

        #region Visualization Queries

        public string GetDateFilteringStringForQuery(VisType type, DateTimeOffset date, string datePropertyName = "time")
        {
            var filter = string.Empty;

            if (type == VisType.Day)
            {
                filter = "STRFTIME('%s', DATE(" + datePropertyName + ")) == STRFTIME('%s', DATE('" + date.Date.ToString("u") + "')) ";
            }
            else if (type == VisType.Week)
            {
                filter = "( "
                    + " STRFTIME('%s', DATE(" + datePropertyName + ")) between STRFTIME('%s', DATE('" + Helpers.DateTimeHelper.GetFirstDayOfWeek_Iso8801(date).Date.ToString("u") 
                    + "')) and STRFTIME('%s', DATE('" + Helpers.DateTimeHelper.GetLastDayOfWeek_Iso8801(date).Date.ToString("u") + "')) "
                    + " ) ";
            }
            return filter;
        }

        public DateTime GetUserWorkStart(DateTimeOffset date)
        {
            var firstEntryDateTime = DateTime.Now; // default value
            try
            {
                var firstEntryReader = new SQLiteCommand("SELECT tsStart FROM " + Settings.WindowsActivityTable +
                                                         " WHERE STRFTIME('%s', DATE(tsStart))==STRFTIME('%s', DATE('" + date.Date.ToString("u") + "'))" +
                                                         " AND STRFTIME('%H', TIME(tsStart)) >= STRFTIME('%H', TIME('04:00:00'))" + // day start should be after 04 am
                                                         " AND process != '" + Dict.Idle +
                                                         "' ORDER BY tsStart ASC LIMIT 1;", _connection).ExecuteReader();

                if (firstEntryReader.HasRows)
                {
                    firstEntryReader.Read(); // read only once
                    firstEntryDateTime = DateTime.Parse((string)firstEntryReader["tsStart"]);
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
                var lastEntryReader = new SQLiteCommand("SELECT tsEnd FROM " + Settings.WindowsActivityTable +
                                                        " WHERE STRFTIME('%s', DATE(time))==STRFTIME('%s', DATE('" +
                                                        date.Date.ToString("u") + "'))" +
                                                        " AND process != '" + Dict.Idle + "' ORDER BY tsEnd DESC LIMIT 1;",
                    _connection).ExecuteReader();

                if (lastEntryReader.HasRows)
                {

                    lastEntryReader.Read(); // read only once
                    lastEntryDateTime = DateTime.Parse((string)lastEntryReader["tsEnd"]);
                }

                lastEntryReader.Close();
            }
            catch (Exception e)
            {
                LogError(e.Message);
            }
            return lastEntryDateTime;
        }

        #endregion

        #region Settings Helper

        /// <summary>
        /// Saves a setting with a given key or value. Inserts new entry if nothing is yet stored
        /// or updates the value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetSettings(string key, string value)
        {
            try
            {
                var query1 = "SELECT value FROM " + Settings.SettingsDbTable + " WHERE key=" + Database.GetInstance().Q(key) + ";";
                var query2 = "INSERT INTO " + Settings.SettingsDbTable + " (key, value) VALUES (" + Database.GetInstance().Q(key) + ", " + Database.GetInstance().Q(value) + ");";
                var query3 = "UPDATE " + Settings.SettingsDbTable + " SET value=" + Database.GetInstance().Q(value) + " WHERE key=" + Database.GetInstance().Q(key) + ";";

                // if key is not yet stored in settings
                var keyStored = Database.GetInstance().ExecuteScalar2(query1);
                if (keyStored == null)
                {
                    Database.GetInstance().ExecuteDefaultQuery(query2);
                }
                // if key is stored in settings
                else
                {
                    Database.GetInstance().ExecuteDefaultQuery(query3);
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Saves a setting with a given key or value. Inserts new entry if nothing is yet stored
        /// or updates the value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetSettings(string key, bool value)
        {
            SetSettings(key, value ? "1" : "0");
        }

        /// <summary>
        /// Gets the stored setting value for a given key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int GetSettingsInt(string key, int byDefault)
        {
            try
            {
                var query = "SELECT value FROM " + Settings.SettingsDbTable + " WHERE key=" + Database.GetInstance().Q(key) + ";";
                var ret = Database.GetInstance().ExecuteScalar(query);

                if (ret > 0) return ret;
                else return byDefault;
            }
            catch
            {
                return byDefault;
            }
        }

        public DateTimeOffset GetSettingsDate(string key, DateTimeOffset byDefault)
        {
            try
            {
                var query = "SELECT value FROM " + Settings.SettingsDbTable + " WHERE key=" + Database.GetInstance().Q(key) + ";";
                var ret = Database.GetInstance().ExecuteScalar2(query);
                if (ret == null) return byDefault;

                var retDt = DateTimeOffset.Parse((string)ret);
                return retDt;
            }
            catch
            {
                return byDefault;
            }
        }

        /// <summary>
        /// Gets the stored setting value for a given key
        /// </summary>
        /// <param name="key"></param>
        /// <returns>true if 1, false if 0</returns>
        public bool GetSettingsBool(string key, bool byDefault)
        {
            try
            {
                var query = "SELECT value FROM " + Settings.SettingsDbTable + " WHERE key=" + Database.GetInstance().Q(key) + ";";
                var ret = Database.GetInstance().ExecuteScalar2(query);

                if (ret == null) return byDefault;
                else return (string)ret == "1";
            }
            catch
            {
                return byDefault;
            }
        }

        /// <summary>
        /// gets the stored setting value for a given key
        /// </summary>
        /// <param name="key"></param>
        /// <param name=""></param>
        /// <returns></returns>
        public string GetSettingsString(string key, string byDefault)
        {
            try
            {
                var query = "SELECT value FROM " + Settings.SettingsDbTable + " WHERE key=" + Database.GetInstance().Q(key) + ";";
                var ret = Database.GetInstance().ExecuteScalar2(query);

                if (ret != null) return ret.ToString();
                else return byDefault;
            }
            catch
            {
                return byDefault;
            }
        }

        /// <summary>
        /// Checks if a settings for a given key is stored.
        /// 
        /// returns false in case of an error
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool HasSetting(string key)
        {
            try
            {
                var query = "SELECT value FROM " + Settings.SettingsDbTable + " WHERE key=" + Database.GetInstance().Q(key) + ";";
                var ret = Database.GetInstance().ExecuteScalar2(query);

                if (ret == null) return false;
                else return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Database Versioning

        /// <summary>
        /// Get the actual database version
        /// </summary>
        /// <returns></returns>
        public int GetDbPragmaVersion()
        {
            var result = ExecuteScalar("PRAGMA user_version;");
            return result;
        }

        /// <summary>
        /// Update the database version when an existing
        /// table changes
        /// </summary>
        /// <param name="version"></param>
        public void UpdateDbPragmaVersion(int version)
        {
            ExecuteDefaultQuery("PRAGMA user_version = " + version);
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
            var dbJustCreated = (File.Exists(CurrentDatabaseDumpFile)) ? false : true;

            // Open the Database connection
            _connection = new SQLiteConnection("Data Source=" + CurrentDatabaseDumpFile);
            _connection.Open();

            // Update database version if db was newly created
            if (dbJustCreated) Database.GetInstance().UpdateDbPragmaVersion(Settings.DatabaseVersion);

            // Create log table if it doesn't exist
            CreateLogTable();

            // Create a settings table if it doesn't exist
            CreateSettingsTable();

            LogInfo(string.Format(CultureInfo.InvariantCulture, "Opened the connection to the database (File={0}).", CurrentDatabaseDumpFile));
        }

        public void Reconnect()
        {
            try
            {
                Disconnect();
                Connect();
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
            //CurrentDatabaseDumpFile = dbFilePath;
        }

        /// <summary>
        /// Closes the connection to the database.
        /// </summary>
        public void Disconnect()
        {
            LogInfo(string.Format(CultureInfo.InvariantCulture, "Closed the connection to the database (File={0}).", CurrentDatabaseDumpFile));
            if (_connection == null || _connection.State == ConnectionState.Closed) return;
            _connection.Close();
            _connection.Dispose();
        }

        /// <summary>
        /// Creates a table for the log inputs (if it doesn't yet exist)
        /// </summary>
        public void CreateLogTable()
        {
            try 
            {
                const string query = "CREATE TABLE IF NOT EXISTS " + Settings.LogDbTable + " (id INTEGER PRIMARY KEY, created INTEGER, message TEXT, type TEXT);";
                ExecuteDefaultQuery(query);
            } 
            catch (Exception e) 
            {
                Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Creates a table for the settings (keys) (if it doesn't yet exist)
        /// </summary>
        public void CreateSettingsTable()
        {
            try
            {
                const string query = "CREATE TABLE IF NOT EXISTS " + Settings.SettingsDbTable + " (id INTEGER PRIMARY KEY, key TEXT, value TEXT);";
                var ans = ExecuteDefaultQuery(query);

                // creating the settings table means the database file was newly created => log this
                if (ans == 0)
                {
                    Database.GetInstance().SetSettings("SettingsTableCreatedDate", DateTime.Now.Date.ToShortDateString());
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        #endregion

        #region TimeZone Tracking

        /// <summary>
        /// Creates a table for the time zone (if it doesn't yet exist)
        /// </summary>
        public void CreateTimeZoneTable()
        {
            try
            {
                const string query = "CREATE TABLE IF NOT EXISTS " + Settings.TimeZoneTable + " (id INTEGER PRIMARY KEY, time TEXT, timezone TEXT, offset TEXT, localTime TEXT, utcTime TEXT)";
                ExecuteDefaultQuery(query);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        public TimeZoneInfo GetLastTimeZoneEntry()
        {
            try
            {
                var reader = new SQLiteCommand("SELECT timezone FROM " + Settings.TimeZoneTable + " ORDER BY time DESC LIMIT 1;", _connection).ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Read(); // read only once
                    var timeZone = TimeZoneInfo.FindSystemTimeZoneById((string)reader["timezone"]);
                    return timeZone;
                }
                reader.Close();
                return null; // no entry or failed to parse
            }
            catch (Exception e)
            {
                LogError(e.Message);
                return null; // other error
            }
        }

        public void LogTimeZoneChange(TimeZoneInfo currentTimeZone)
        {
            try
            {
                Database.GetInstance().ExecuteDefaultQuery("INSERT INTO " + Settings.TimeZoneTable + " (time, timezone, offset, localTime, utcTime) VALUES (strftime('%Y-%m-%d %H:%M:%f', 'now', 'localtime'), " +
                Q(currentTimeZone.Id) + ", " + Q(currentTimeZone.BaseUtcOffset.ToString()) + ", " + QTime(DateTime.Now.ToLocalTime()) + ", " + QTime(DateTime.Now.ToUniversalTime()) + ")");

                Logger.WriteToConsole(string.Format(CultureInfo.InvariantCulture, "TimeZoneInfo: local=[{0}], utc=[{1}], zone=[{2}], offset=[{3}].",
                   DateTime.Now.ToLocalTime(), DateTime.Now.ToUniversalTime(), currentTimeZone.Id, currentTimeZone.BaseUtcOffset));
            } 
            catch (Exception e)
            {
                LogError(e.Message);
            }
        }

        #endregion
    }
}
 