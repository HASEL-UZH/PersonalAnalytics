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
        private static readonly string DIFFERENCE_RRINTERVAL = "rrdifference";

        private static readonly string TABLE_NAME = "biometrics";
        private static readonly string CREATE_QUERY = "CREATE TABLE IF NOT EXISTS " + TABLE_NAME + " (" + ID + " INTEGER PRIMARY KEY, " + TIME + " TEXT, " + HEARTRATE + " INTEGER, " + RRINTERVAL + " DOUBLE, " + DIFFERENCE_RRINTERVAL + "DOUBLE )";

        private static readonly string INSERT_QUERY = "INSERT INTO " + TABLE_NAME + "(" + TIME + ", " + HEARTRATE + ", " + RRINTERVAL + ", " + DIFFERENCE_RRINTERVAL + ") VALUES ('{0}', {1}, {2}, {3})";

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
            double previousRRInterval = GetLastRRInterval();

            try
            {
                string query = string.Empty;
                if (Double.IsNaN(heartrate))
                {
                    if (Double.IsNaN(previousRRInterval))
                    {
                        query += String.Format(INSERT_QUERY, timestamp, "null", rrInterval, "null");
                    }
                    else
                    {
                        query += String.Format(INSERT_QUERY, timestamp, "null", rrInterval, Math.Abs(rrInterval - previousRRInterval));
                    }
                }
                else
                {
                    if (Double.IsNaN(previousRRInterval))
                    {
                        query += String.Format(INSERT_QUERY, timestamp, heartrate, rrInterval, "null");
                    }
                    else
                    {
                        query += String.Format(INSERT_QUERY, timestamp, heartrate, rrInterval, Math.Abs(rrInterval - previousRRInterval));
                    }
                }

                Database.GetInstance().ExecuteDefaultQuery(query);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        private static double GetLastRRInterval()
        {
            var query = "Select " + RRINTERVAL + " From " + TABLE_NAME + " ORDER BY ID DESC LIMIT 1;";
            var table = Database.GetInstance().ExecuteReadQuery(query);
            if (table.Rows.Count != 1)
            {
                table.Dispose();
                return Double.NaN;
            }
            else
            {
                double value = Double.NaN;
                double.TryParse(table.Rows[0][0].ToString(), out value);
                table.Dispose();
                return value;
            }
        }

        #region day
        internal static List<Tuple<DateTime, double, double>> GetBiometricValuesForDay(DateTimeOffset date)
        {
            List<Tuple<DateTime, double, double>> result = new List<Tuple<DateTime, double, double>>();

            var query = "SELECT " + "STRFTIME('%Y-%m-%d %H:%M', " + TIME + ")" + ", AVG(" + HEARTRATE + "), AVG(" + DIFFERENCE_RRINTERVAL + "*" + DIFFERENCE_RRINTERVAL + ") FROM " + TABLE_NAME + " WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, TIME) + "GROUP BY strftime('%H:%M', " + TIME + ");";
            
            var table = Database.GetInstance().ExecuteReadQuery(query);

            foreach (DataRow row in table.Rows)
            {
                var timestamp = (String)row[0];
                
                double hr = Double.NaN;
                double.TryParse(row[1].ToString(), out hr);

                double rmssd = Double.NaN;
                if (row[2] != DBNull.Value)
                {
                    double.TryParse(row[2].ToString(), out rmssd);
                    if (!Double.IsNaN(rmssd))
                    {
                        rmssd = Math.Sqrt(rmssd);
                    }
                }
                result.Add(new Tuple<DateTime, double, double>(DateTime.ParseExact(timestamp, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), hr, rmssd));
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