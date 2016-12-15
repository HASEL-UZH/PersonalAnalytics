// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using System;
using Shared;
using Shared.Data;
using System.Data;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BiometricsTracker.Data
{
    public class DatabaseConnector
    {
        private static readonly string ID = "id";
        private static readonly string TIME = "time";
        private static readonly string HEARTRATE = "heartrate";
        private static readonly string RRINTERVAL = "rr";

        private static readonly string TABLE_NAME = "biometrics";
        private static readonly string CREATE_QUERY = "CREATE TABLE IF NOT EXISTS " + TABLE_NAME + " (" + ID + " INTEGER PRIMARY KEY, " + TIME + " TEXT, " + HEARTRATE + " INTEGER, " + RRINTERVAL + " DOUBLE)";

        private static readonly string INSERT_QUERY = "INSERT INTO " + TABLE_NAME + "(" + TIME + ", " + HEARTRATE + ", " + RRINTERVAL + ") VALUES ('{0}', {1}, {2})";

        internal static void CreateBiometricTables()
        {
            try
            {
                Database.GetInstance().ExecuteDefaultQuery(CREATE_QUERY);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        internal static void AddHeartMeasurementToDatabase(String timestamp, double heartrate, double rrInterval)
        {
            try
            {
                string query = string.Empty;
                if (Double.IsNaN(heartrate))
                {
                    query += String.Format(INSERT_QUERY, timestamp, "null", rrInterval);
                }
                else
                {
                    query += String.Format(INSERT_QUERY, timestamp, heartrate, rrInterval);
                }
                
                Database.GetInstance().ExecuteDefaultQuery(query);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        #region day

        internal static double GetAverageHeartrateForDay(DateTimeOffset date)
        {
            return GetSpecificBiometricValueForDay(date, HEARTRATE).Average();
        }

        internal static double GetAverageHeartrateVariabilityForDay(DateTimeOffset date)
        {
            return GetSpecificBiometricValueForDay(date, RRINTERVAL).Average();
        }

        private static List<double> GetSpecificBiometricValueForDay(DateTimeOffset date, string column)
        {
            List<double> values = new List<double>();
            var query = "SELECT AVG(" + column + ") FROM " + TABLE_NAME + " WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, TIME) + "GROUP BY strftime('%H:%M', " + TIME + ");";
            var table = Database.GetInstance().ExecuteReadQuery(query);
            foreach (DataRow row in table.Rows)
            {
                double value = Double.NaN;
                double.TryParse(row[0].ToString(), out value);

                values.Add(value);
            }
            table.Dispose();
            return values;
        }

        internal static List<Tuple<DateTime, double, double>> GetBiometricValuesForDay(DateTimeOffset date)
        {
            List<Tuple<DateTime, double, double>> result = new List<Tuple<DateTime, double, double>>();

            var query = "SELECT " + "STRFTIME('%Y-%m-%d %H:%M', " + TIME + ")" + ", AVG(" + HEARTRATE + "), AVG(" + RRINTERVAL + ") FROM " + TABLE_NAME + " WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, TIME) + "GROUP BY strftime('%H:%M', " + TIME + ");";
            
            var table = Database.GetInstance().ExecuteReadQuery(query);

            foreach (DataRow row in table.Rows)
            {
                var timestamp = (String)row[0];
                
                double hr = Double.NaN;
                double.TryParse(row[1].ToString(), out hr);

                double rr = Double.NaN;
                double.TryParse(row[2].ToString(), out rr);
                result.Add(new Tuple<DateTime, double, double>(DateTime.ParseExact(timestamp, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), hr, rr));
            }
            table.Dispose();

            return result;
        }

        #endregion

        #region week

        internal static List<Tuple<DateTime, double>> GetHRVValuesForWeek(DateTimeOffset date)
        {
            return GetBiometricValuesForWeek(date, RRINTERVAL);
        }

        internal static List<Tuple<DateTime, double>> GetHRValuesForWeek(DateTimeOffset date)
        {
            return GetBiometricValuesForWeek(date, HEARTRATE);
        }

        private static List<Tuple<DateTime, double>> GetBiometricValuesForWeek(DateTimeOffset date, String column)
        {
            var result = new List<Tuple<DateTime, double>>();

            var query = "SELECT strftime('%Y-%m-%d %H'," + TIME + "), avg(" + column + ") FROM " + TABLE_NAME + " WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Week, date, TIME) + " GROUP BY strftime('%Y-%m-%d %H',time);";
            var table = Database.GetInstance().ExecuteReadQuery(query);

            foreach (DataRow row in table.Rows)
            {
                var timestamp = (String)row[0];
                double rr = Double.NaN;
                double.TryParse(row[1].ToString(), out rr);
                result.Add(new Tuple<DateTime, double>(DateTime.ParseExact(timestamp, "yyyy-MM-dd H", CultureInfo.InvariantCulture), rr));
            }
            table.Dispose();

            return result;
        }

        #endregion
    }
}