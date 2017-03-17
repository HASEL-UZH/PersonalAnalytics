// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using System;
using System.Globalization;

namespace Shared.Helpers
{
    public static class DateTimeHelper
    {
        public static DateTimeOffset GetStartOfDay(this DateTimeOffset date)
        {
            return date.Date;
        }

        public static DateTimeOffset GetEndOfDay(this DateTimeOffset date)
        {
            return date.Date.AddDays(1).AddTicks(-1);
        }

        /// <summary>
        /// This presumes that weeks start with Monday.
        /// Week 1 is the 1st week of the year with a Thursday in it.
        /// see: http://stackoverflow.com/questions/11154673/get-the-correct-week-number-of-a-given-date
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static int GetWeekOfYear_Iso8601(DateTime time)
        {
            // Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll 
            // be the same week# as whatever Thursday, Friday or Saturday are,
            // and we always get those right
            var day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }

            // Return the week of our adjusted day
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        /// <summary>
        /// For a given date, return the week's start date
        /// see: http://stackoverflow.com/questions/662379/calculate-date-from-week-number
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTimeOffset GetFirstDayOfWeek_Iso8801(DateTimeOffset date)
        {
            DateTime jan1 = new DateTime(date.Year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            DateTime firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = GetWeekOfYear_Iso8601(date.Date);
            if (firstWeek <= 1)
            {
                weekNum -= 1;
            }
            var result = firstThursday.AddDays(weekNum * 7);
            return result.AddDays(-3);

            //var ci = CultureInfo.CurrentCulture;
            //var weekOfYear = GetIso8601WeekOfYear(date.Date);
            //DateTime jan1 = new DateTime(date.Year, 1, 1);
            //int daysOffset = (int)ci.DateTimeFormat.FirstDayOfWeek - (int)jan1.DayOfWeek;
            //DateTime firstWeekDay = jan1.AddDays(daysOffset);
            //int firstWeek = ci.Calendar.GetWeekOfYear(jan1, ci.DateTimeFormat.CalendarWeekRule, ci.DateTimeFormat.FirstDayOfWeek);
            //if (firstWeek <= 1 || firstWeek > 50)
            //{
            //    weekOfYear -= 1;
            //}
            //return firstWeekDay.AddDays(weekOfYear * 7);
        }

        /// <summary>
        /// For a given date, return the week's last date
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTimeOffset GetLastDayOfWeek_Iso8801(DateTimeOffset date)
        {
            var firstDay = GetFirstDayOfWeek_Iso8801(date);
            return firstDay.AddDays(6);
        }

        /// <summary>
        /// Returns the shortest day name for a given date according to the current culture
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string GetShortestDayName(DateTimeOffset date)
        {
            string[] names = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;
            return names[(int)date.DayOfWeek];
        }

        /// <summary>
        /// Returns the shortest day name for a given week day number according to the current culture
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string GetShortestDayName(int dayNumber)
        {
            string[] names = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;
            return names[dayNumber];
        }

        /// <summary>
        /// Rounds up a time (e.g. 10minutes)
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public static DateTime RoundUp(DateTime dt, TimeSpan d)
        {
            return new DateTime(((dt.Ticks + d.Ticks - 1) / d.Ticks) * d.Ticks);
        }

        /// <summary>
        /// JavaScript Timestamp from C# DateTime 
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static long JavascriptTimestampFromDateTime(DateTime date)
        {
            var datetimeMinTimeTicks = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks;
            return ((date.ToUniversalTime().Ticks - datetimeMinTimeTicks) / 10000);
            // return (date.Ticks - 621355968000000000)/10000; //old: had wrong timezone
        }

        /// <summary>
        /// C# DateTime from JavaScript Timestamp
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime DateTimeFromJavascriptTimestamp(long date)
        {
            var datetimeMinTimeTicks = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks;
            var newUniversalTime = new DateTime((date*10000) + datetimeMinTimeTicks).ToUniversalTime();
            return newUniversalTime;
        }
    }
}
