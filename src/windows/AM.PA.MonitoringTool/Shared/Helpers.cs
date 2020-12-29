// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.Globalization;

namespace Shared
{
    public static class Helpers
    {
        /// <summary>
        /// This presumes that weeks start with Monday.
        /// Week 1 is the 1st week of the year with a Thursday in it.
        /// see: http://stackoverflow.com/questions/11154673/get-the-correct-week-number-of-a-given-date
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static int GetIso8601WeekOfYear(DateTime time)
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

        public static DateTime DateTimeFromJavascriptTimestamp(long date)
        {
            var datetimeMinTimeTicks = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks;
            var newUniversalTime = new DateTime((date*10000) + datetimeMinTimeTicks).ToUniversalTime();
            return newUniversalTime;
        }

        /// <summary>
        /// Makes every string's first letter upper case
        /// the rest lower case
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string FirstLetterToUpper(string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1).ToLower();

            return str.ToUpper();
        }

    }
}
