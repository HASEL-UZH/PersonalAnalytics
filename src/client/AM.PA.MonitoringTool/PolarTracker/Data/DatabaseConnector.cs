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
using BluetoothLowEnergy;

namespace PolarTracker.Data
{
    public class DatabaseConnector
    {
        private const string ID = "id";
        private const string TIME = "time";
        private const string HEARTRATE = "heartrate";
        private const string RRINTERVAL = "rr";
        private const string DIFFERENCE_RRINTERVAL = "rrdifference";
        
        private static readonly string CREATE_QUERY = "CREATE TABLE IF NOT EXISTS " + Settings.TABLE_NAME + " (" + ID + " INTEGER PRIMARY KEY, " + TIME + " TEXT, " + HEARTRATE + " INTEGER, " + RRINTERVAL + " DOUBLE, " + DIFFERENCE_RRINTERVAL + " DOUBLE)";
        private static readonly string CREATE_AGGREGATED_QUERY = "CREATE TABLE IF NOT EXISTS " + Settings.TABLE_NAME_AGGREGATED + " (" + ID + " INTEGER PRIMARY KEY, " + TIME + " TEXT, " + HEARTRATE + " INTEGER, " + RRINTERVAL + " DOUBLE, " + DIFFERENCE_RRINTERVAL + " DOUBLE)";

        private static readonly string INSERT_QUERY = "INSERT INTO " + Settings.TABLE_NAME + "(" + TIME + ", " + HEARTRATE + ", " + RRINTERVAL + ", " + DIFFERENCE_RRINTERVAL + ") VALUES ('{0}', {1}, {2}, {3})";
        private static readonly string INSERT_QUERY_AGGREGATED = "INSERT INTO " + Settings.TABLE_NAME_AGGREGATED + "(" + TIME + ", " + HEARTRATE + ", " + RRINTERVAL + ", " + DIFFERENCE_RRINTERVAL + ") VALUES ('{0}', {1}, {2}, {3})";

        private static readonly string INSERT_QUERY_MULTIPLE_VALUES = "INSERT INTO " + Settings.TABLE_NAME + " SELECT null AS " + ID + ", " + "'{0}' AS " + TIME + ", {1} AS " + HEARTRATE + ", {2} AS " + RRINTERVAL + ", {3} AS " + DIFFERENCE_RRINTERVAL;
        private static readonly string INSERT_QUERY_MULTIPLE_VALUES_AGGREGATED = "INSERT INTO " + Settings.TABLE_NAME_AGGREGATED + " SELECT null AS " + ID + ", " + "'{0}' AS " + TIME + ", {1} AS " + HEARTRATE + ", {2} AS " + RRINTERVAL + ", {3} AS " + DIFFERENCE_RRINTERVAL;

        #region create
        internal static void CreatePolarTables()
        {
            try
            {
                Database.GetInstance().ExecuteDefaultQuery(CREATE_AGGREGATED_QUERY);
               
                if (Settings.IsDetailedCollectionEnabled)
                {
                    Database.GetInstance().ExecuteDefaultQuery(CREATE_QUERY);
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }
        #endregion

        #region insert
        
        internal static void AddHeartMeasurementsToDatabase(List<HeartRateMeasurement> measurements, bool aggregated)
        {
             try
            {
                if (measurements.Count == 0)
                {
                    return;
                }
                else if (measurements.Count == 1)
                {
                    string query = string.Empty;

                    if (!aggregated)
                    {
                        query += String.Format(INSERT_QUERY, measurements[0].Timestamp, Double.IsNaN(measurements[0].HeartRateValue) ? "null" : measurements[0].HeartRateValue.ToString(), measurements[0].RRInterval, Double.IsNaN(measurements[0].RRDifference) ? "null" : measurements[0].RRDifference.ToString());
                    }
                    else
                    {
                        query += String.Format(INSERT_QUERY_AGGREGATED, measurements[0].Timestamp, Double.IsNaN(measurements[0].HeartRateValue) ? "null" : measurements[0].HeartRateValue.ToString(), measurements[0].RRInterval, Double.IsNaN(measurements[0].RRDifference) ? "null" : measurements[0].RRDifference.ToString());
                    }
                    Database.GetInstance().ExecuteDefaultQuery(query);
                }
                else
                {
                    string query = string.Empty;

                    if (!aggregated)
                    {
                        query += String.Format(INSERT_QUERY_MULTIPLE_VALUES, measurements[0].Timestamp, Double.IsNaN(measurements[0].HeartRateValue) ? "null" : measurements[0].HeartRateValue.ToString(), measurements[0].RRInterval, Double.IsNaN(measurements[0].RRDifference) ? "null" : measurements[0].RRDifference.ToString());
                    }
                    else
                    {
                        query += String.Format(INSERT_QUERY_MULTIPLE_VALUES_AGGREGATED, measurements[0].Timestamp, Double.IsNaN(measurements[0].HeartRateValue) ? "null" : measurements[0].HeartRateValue.ToString(), measurements[0].RRInterval, Double.IsNaN(measurements[0].RRDifference) ? "null" : measurements[0].RRDifference.ToString());
                    }

                    for (int i = 1; i < measurements.Count; i++)
                    {
                        query += String.Format(" UNION ALL SELECT null, '{0}', {1}, {2}, {3}", measurements[i].Timestamp, Double.IsNaN(measurements[i].HeartRateValue) ? "null" : measurements[i].HeartRateValue.ToString(), measurements[i].RRInterval, Double.IsNaN(measurements[i].RRDifference) ? "null" : measurements[i].RRDifference.ToString());
                    }

                    Logger.WriteToConsole(query);

                    Database.GetInstance().ExecuteDefaultQuery(query);
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }
        #endregion

        #region day
        internal static List<Tuple<DateTime, double, double>> GetPolarValuesForDay(DateTimeOffset date)
        {
            List<Tuple<DateTime, double, double>> result = new List<Tuple<DateTime, double, double>>();

            try
            {
                string tableName = Settings.TABLE_NAME_AGGREGATED;

                var query = "SELECT " + "STRFTIME('%Y-%m-%d %H:%M', " + TIME + ")" + ", AVG(" + HEARTRATE + "), AVG(" + DIFFERENCE_RRINTERVAL + "*" + DIFFERENCE_RRINTERVAL + ") FROM " + tableName + " WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, TIME) + "GROUP BY strftime('%H:%M', " + TIME + ");";
                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    var timestamp = (String)row[0];

                    double hr = Double.NaN;
                    double.TryParse(row[1].ToString(), out hr);
                    if (IsAboveThresholdValue(hr, HEARTRATE))
                    {
                        hr = Double.NaN;
                    }

                    double rmssd = Double.NaN;
                    if (row[2] != DBNull.Value)
                    {
                        double.TryParse(row[2].ToString(), out rmssd);

                        if (rmssd > 1)
                        {
                            rmssd = Double.NaN;
                        }

                        if (!Double.IsNaN(rmssd))
                        {
                            rmssd = Math.Sqrt(rmssd);
                            rmssd *= 1000;
                        }
                    }
                    result.Add(new Tuple<DateTime, double, double>(DateTime.ParseExact(timestamp, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), hr, rmssd));
                }
                table.Dispose();
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
            return result;
        }
        #endregion

        #region week
        internal static List<Tuple<DateTime, double>> GetHRVValuesForWeek(DateTimeOffset date)
        {
            return GetPolarValuesForWeek(date, RRINTERVAL);
        }

        internal static List<Tuple<DateTime, double>> GetHRValuesForWeek(DateTimeOffset date)
        {
            return GetPolarValuesForWeek(date, HEARTRATE);
        }
        
        private static List<Tuple<DateTime, double>> GetPolarValuesForWeek(DateTimeOffset date, String column)
        {
            var result = new List<Tuple<DateTime, double>>();

            try
            {
                string tableName = Settings.TABLE_NAME_AGGREGATED;

                var query = "SELECT strftime('%Y-%m-%d %H'," + TIME + "), avg(" + column + ") FROM " + tableName + " WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Week, date, TIME) + "AND " + column + " <= " + GetThresholdValue(column) + " GROUP BY strftime('%Y-%m-%d %H',time);";
                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    var timestamp = (String)row[0];
                    double value = Double.NaN;
                    if (row[1] != DBNull.Value)
                    {
                        double.TryParse(row[1].ToString(), out value);
                    }
                    result.Add(new Tuple<DateTime, double>(DateTime.ParseExact(timestamp, "yyyy-MM-dd H", CultureInfo.InvariantCulture), value));
                }
                table.Dispose();
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
            return result;
        }

        #region Helper

        internal static List<Tuple<DateTime, double>> GetRMSSDValuesForWeek(DateTimeOffset date)
        {
            var result = new List<Tuple<DateTime, double>>();

            try
            {
                //Go back to Monday
                while (date.DayOfWeek != DayOfWeek.Monday)
                {
                    date = date.AddDays(-1);
                }

                //Iterate over whole week
                while (date.DayOfWeek != DayOfWeek.Sunday)
                {
                    result.AddRange(CalculateHourAverage(GetPolarValuesForDay(date)));
                    date = date.AddDays(1);
                }

                //Iterate Sunday
                result.AddRange(CalculateHourAverage(GetPolarValuesForDay(date)));

            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
            return result;
        }

        private static List<Tuple<DateTime, double>> CalculateHourAverage(List<Tuple<DateTime, double, double>> values)
        {
            var result = new List<Tuple<DateTime, double>>();

            try
            {
                if (values.Count > 0)
                {
                    DateTime firstHour = values[0].Item1;
                    DateTime lastHour = values[values.Count - 1].Item1;

                    if (firstHour.Hour == lastHour.Hour)
                    {
                        SumUpValuesForOneHour(values, result, firstHour);
                    }
                    else
                    {
                        while (firstHour.Hour.CompareTo(lastHour.Hour + 1) != 0)
                        {
                            SumUpValuesForOneHour(values, result, firstHour);
                            firstHour = firstHour.AddHours(1);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
            return result;
        }

        private static void SumUpValuesForOneHour(List<Tuple<DateTime, double, double>> values, List<Tuple<DateTime, double>> result, DateTime firstHour)
        {
            List<Tuple<DateTime, double, double>> tuplesForThisHour = values.FindAll(t => t.Item1.Hour.CompareTo(firstHour.Hour) == 0);

            double sum = 0;
            double count = 0;

            foreach (Tuple<DateTime, double, double> t in tuplesForThisHour)
            {
                if (!Double.IsNaN(t.Item3))
                {
                    sum += t.Item3;
                    count++;
                }
            }

            Tuple<DateTime, double> createTuple = new Tuple<DateTime, double>(firstHour, sum / count);
            result.Add(createTuple);
        }

        private static double GetThresholdValue(string column)
        {
            switch(column)
            {
                case HEARTRATE:
                    return Settings.HEARTRATE_THRESHOLD;
                    
                case DIFFERENCE_RRINTERVAL:
                    return Settings.RR_DIFFERENCE_THRESHOLD;
            }
            return Double.MaxValue;
        }

        private static bool IsAboveThresholdValue(double value, string column)
        {
            switch (column)
            {
                case HEARTRATE:
                    return value > Settings.HEARTRATE_THRESHOLD;

                case DIFFERENCE_RRINTERVAL:
                    return value > Settings.RR_DIFFERENCE_THRESHOLD;
            }
            return false;
        }
        #endregion

        #endregion

        #region SELECT

        internal static DateTime GetLastTimeSynced()
        {
            string tableName = Settings.TABLE_NAME_AGGREGATED;
            var query = "SELECT " + TIME + " FROM " + tableName + " ORDER BY " + TIME + " DESC LIMIT 1";
            var table = Database.GetInstance().ExecuteReadQuery(query);

            if (table.Rows.Count == 0)
            {
                return DateTime.MinValue;
            }

            DataRow row = table.Rows[0];
            var timestamp = (string)row[0];
            return DateTime.Parse(timestamp);
        }

        #endregion
    }
}