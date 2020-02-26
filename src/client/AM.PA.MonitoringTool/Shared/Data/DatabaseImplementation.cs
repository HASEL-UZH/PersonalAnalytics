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
using System.Linq;

namespace Shared.Data
{
    internal enum LogType { Info, Warning, Error }

    /// <inheritdoc />
    /// <summary>
    /// This is the implementation of the database with all queries, commands, etc.
    /// </summary>
    public sealed class DatabaseImplementation : IDisposable
    {
        private SQLiteConnection _connection; // null if not connected
        private readonly string _currentDatabaseDumpFile;
        public const string DB_FORMAT_DAY_AND_TIME = "yyyy-MM-dd HH:mm:ss";
        private readonly object _batchLock = new object();

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
            if (!IsConnected())
            {
                throw new Exception("Connection to database not established.");
            }
            try
            {
                const string query = "SELECT name FROM sqlite_master WHERE type='table' AND name=?;";
                var result = ExecuteReadQuery(query, new object[] { tableName });
                return result != null && result.Rows.Count != 0;
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
        /// <param name="parameter"></param>
        /// <returns>The number of affected rows or 0 if unsuccessful</returns>
        public int ExecuteDefaultQuery(string query, object[] parameter = null)
        {
            SQLiteCommand cmd = null;
            try
            {
                cmd = GetCommand(query, parameter);
                var affectedRowCount = cmd.ExecuteNonQuery();
                return affectedRowCount;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                var message = $@"Query: {query}";
                if (parameter != null)
                    message += $"\r\nParameter: [{string.Join(", ", parameter)}]";
                Logger.WriteToLogFile(new Exception(message));
                return 0;
            }
            finally
            {
                cmd?.Dispose();
            }
        }

        /// <summary>
        /// Executes a query scalar. Returns the INT in case of an entry, 
        /// or 0 in case of no entry or an error.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public int ExecuteScalar(string query, object[] parameter = null)
        {
            SQLiteCommand cmd = null;
            try
            {
                cmd = GetCommand(query, parameter);
                var res = cmd.ExecuteScalar();
                return DBNull.Value != res ? Convert.ToInt32(res) : 0;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return 0;
            }
            finally
            {
                cmd?.Dispose();
            }
        }

        /// <summary>
        /// Executes a query scalar and returns null if there was an error (or no entry)
        /// used for settings. Somehow strange?
        /// </summary>
        /// <param name="query"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public object ExecuteScalar2(string query, object[] parameter = null)
        {
            SQLiteCommand cmd = null;
            try
            {
                cmd = GetCommand(query, parameter);
                return cmd.ExecuteScalar();
            }
            catch
            {
                return null;
            }
            finally
            {
                cmd?.Dispose();
            }
        }

        /// <summary>
        /// Executes a query scalar. Return the DOUBLE in case of an entry, 
        /// or 0.0 in case of no entry or an error.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public double ExecuteScalar3(string query, object[] parameter = null)
        {
            SQLiteCommand cmd = null;
            try
            {
                cmd = GetCommand(query, parameter);
                var res = cmd.ExecuteScalar();
                return DBNull.Value != res ? Convert.ToDouble(res) : 0.0;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return 0.0;
            }
            finally
            {
                cmd?.Dispose();
            }
        }

        /// <summary>
        /// Executes a query (given as a parameter).
        /// </summary>
        /// <param name="query"></param>
        /// <param name="parameter"></param>
        /// <returns>the first table as a result or null if there was no result</returns>
        public DataTable ExecuteReadQuery(string query, object[] parameter = null)
        {
            SQLiteCommand cmd = null;
            try
            {
                cmd = GetCommand(query, parameter);
                var da = new SQLiteDataAdapter(cmd);
                var ds = new DataSet();
                da.Fill(ds);
                return ds.Tables[0];
            }
            catch
            {
                return null;
            }
            finally
            {
                cmd?.Dispose();
            }
        }

        /// <summary>
        /// Executes a query for each set of parameters inside a transaction.
        /// Provides consistency as well as performance improvements when used for batch inserts.
        /// </summary>
        /// <param name="query">Query with ?-placeholders.</param>
        /// <param name="parameterList">List of parameter values.</param>
        /// <returns>Returns the sum of the number of affected rows of all queries or 0 in case of failure.</returns>
        public int ExecuteBatchQueries(string query, IEnumerable<object[]> parameterList)
        {
            var affectedRowCount = 0;
            try
            {
                lock (_batchLock)
                {
                    using (var transaction = _connection.BeginTransaction())
                    {
                        affectedRowCount = parameterList.Sum(parameter => ExecuteDefaultQuery(query, parameter));
                        transaction.Commit();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
            return affectedRowCount;
        }

        private SQLiteCommand GetCommand(string query, object[] parameter = null)
        {
            WriteQueryToLog(query, parameter);
            if (!IsConnected())
            {
                throw new Exception("Connection has to be opened");
            }
            var cmd = new SQLiteCommand(query, _connection);
            if (parameter != null)
            {
                foreach (var parameterValue in parameter)
                {
                    cmd.Parameters.AddWithValue(null, parameterValue);
                }
            }
            return cmd;
        }

        /// <summary>
        /// Inserts the message (given as a parameter) into the log-database-table (flag: Error).
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        private void LogToDatabase(string message, LogType type)
        {
            try
            {
                WriteQueryToLog(message, null, true);
                var query = $@"INSERT INTO {Settings.LogDbTable} (created, message, type) VALUES (strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'), ?, ?)";
                ExecuteDefaultQuery(query, new object[] { message, type.ToString() });
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Inserts the message (given as a parameter) into the log-database-table (flag: Error).
        /// </summary>
        /// <param name="message"></param>
        public void LogError(string message)
        {
            LogToDatabase(message, LogType.Error);
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
            LogToDatabase(message, LogType.Info);
        }

        /// <summary>
        /// Inserts the message (given as a parameter) into the log-database-table (flag: Warning).
        /// Also logs the query.
        /// </summary>
        /// <param name="message"></param>
        public void LogWarning(string message)
        {
            LogToDatabase(message, LogType.Warning);
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
        /// Q is the James-Bond guy. Joins the string list together and improves it for query string as to allow us to write the content into a single database column
        /// </summary>
        /// <param name="strlist"></param>
        /// <returns></returns>
        public string Q(List<string> strlist)
        {
            string joinedString = string.Join(";", strlist.ToArray());
            return Q(joinedString);
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
            if (str) return "'" + 1 + "'";
            return "'" + 0 + "'";
        }

        /// <summary>
        /// Formats and magicifies a datetime
        /// '%Y-%m-%d %H:%M:%S'
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public string QTime(DateTime dateTime)
        {
            var dateTimeString = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            return Q(dateTimeString);
        }

        /// <summary>
        /// Formats and magicifies a datetime
        /// '%Y-%m-%d %H:%M:%S'
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public string QTime(TimeSpan dateTime)
        {
            var dateTimeString = dateTime.ToString();
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
            var dateTimeString = dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
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
            var dateTimeString = dateTime.ToString("yyyy-MM-dd");
            return Q(dateTimeString);
        }

        /// <summary>
        /// Logs the query if the global setting allows it and it's not
        /// enforced by the calling method.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="parameter"></param>
        /// <param name="force"></param>
        private static void WriteQueryToLog(string query, object[] parameter = null, bool force = false)
        {
            // ReSharper disable once RedundantLogicalConditionalExpressionOperand
            if (Settings.PrintQueriesToConsole == false && force == false) return;
            var message = $@"Query: {query}";
            if (parameter != null)
                message += $"\r\nParameter: [{string.Join(", ", parameter)}]";
            Logger.WriteToConsole(message);
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
                var hasKeyQuery = $@"SELECT value FROM {Settings.SettingsDbTable} WHERE key=?;";
                var insertKeyQuery = $@"INSERT INTO {Settings.SettingsDbTable} (key, value) VALUES (?, ?);";
                var updateKeyQuery = $@"UPDATE {Settings.SettingsDbTable} SET value=? WHERE key=?;";

                if (ExecuteScalar2(hasKeyQuery, new object[] { key }) == null)
                {
                    ExecuteDefaultQuery(insertKeyQuery, new object[] { key, value });
                }
                else
                {
                    ExecuteDefaultQuery(updateKeyQuery, new object[] { value, key });
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
        /// <param name="byDefault"></param>
        /// <returns></returns>
        public int GetSettingsInt(string key, int byDefault)
        {
            try
            {
                var query = $@"SELECT value FROM {Settings.SettingsDbTable} WHERE key=?;";
                var ret = ExecuteScalar(query, new object[] { key });
                return ret > 0 ? ret : byDefault;
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
                var query = $@"SELECT value FROM {Settings.SettingsDbTable} WHERE key=?;";
                var ret = ExecuteScalar2(query, new object[] { key });
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
        /// <param name="byDefault"></param>
        /// <returns>true if 1, false if 0</returns>
        public bool GetSettingsBool(string key, bool byDefault)
        {
            try
            {
                var query = $@"SELECT value FROM {Settings.SettingsDbTable} WHERE key=?;";
                var ret = ExecuteScalar2(query, new object[] { key });
                if (ret == null) return byDefault;
                return (string)ret == "1";
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
        /// <param name="byDefault"></param>
        /// <returns></returns>
        public string GetSettingsString(string key, string byDefault)
        {
            try
            {
                var query = $@"SELECT value FROM {Settings.SettingsDbTable} WHERE key=?;";
                var ret = ExecuteScalar2(query, new object[] { key });
                return ret != null ? ret.ToString() : byDefault;
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
                var query = $@"SELECT value FROM {Settings.SettingsDbTable} WHERE key=?;";
                var ret = ExecuteScalar2(query, new object[] { key });
                return ret != null;
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
            ExecuteDefaultQuery($@"PRAGMA user_version = {version}");
        }

        #endregion

        #region Other Database stuff (Connect, Disconnect, create Log table, Singleton, etc.)

        public DatabaseImplementation(string dbFilePath)
        {
            _currentDatabaseDumpFile = dbFilePath;
        }

        /// <summary>
        /// Opens a connection to the database with the current database save path
        /// </summary>
        public void Connect()
        {
            var dbJustCreated = !File.Exists(_currentDatabaseDumpFile);

            // Open the Database connection
            _connection = new SQLiteConnection($@"Data Source={_currentDatabaseDumpFile}");
            _connection.Open();

            // Update database version if db was newly created
            if (dbJustCreated) UpdateDbPragmaVersion(Settings.DatabaseVersion);

            // Create log table if it doesn't exist
            CreateLogTable();

            // Create a settings table if it doesn't exist
            CreateSettingsTable();

            LogInfo($@"Opened the connection to the database (File = { _currentDatabaseDumpFile}).");
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
        }

        /// <summary>
        /// Closes the connection to the database.
        /// </summary>
        public void Disconnect()
        {
            LogInfo($@"Closed the connection to the database (File = { _currentDatabaseDumpFile}).");
            if (_connection == null || _connection.State == ConnectionState.Closed) return;
            _connection.Close();
            _connection.Dispose();
            _connection = null;
        }

        private bool IsConnected()
        {
            return _connection != null && _connection.State == ConnectionState.Open;
        }

        /// <summary>
        /// Creates a table for the log inputs (if it doesn't yet exist)
        /// </summary>
        public void CreateLogTable()
        {
            try
            {
                var query = $@"CREATE TABLE IF NOT EXISTS {Settings.LogDbTable} (id INTEGER PRIMARY KEY, created INTEGER, message TEXT, type TEXT);";
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
                var query = $@"CREATE TABLE IF NOT EXISTS {Settings.SettingsDbTable} (id INTEGER PRIMARY KEY, key TEXT, value TEXT);";
                var affectedRowCount = ExecuteDefaultQuery(query);

                // creating the settings table means the database file was newly created => log this
                if (affectedRowCount == 0)
                {
                    SetSettings("SettingsTableCreatedDate", DateTime.Now.Date.ToShortDateString());
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
                ExecuteDefaultQuery("INSERT INTO " + Settings.TimeZoneTable + " (time, timezone, offset, localTime, utcTime) VALUES (strftime('%Y-%m-%d %H:%M:%f', 'now', 'localtime'), " +
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
