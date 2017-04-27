// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-04-27
// 
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Shared.Helpers
{
    public static class DateTimeExtensions
    {
        public static string ToHourString(this DateTime dt)
        {
            return dt.ToHourString(null);
        }

        public static string ToHourString(this DateTime dt, IFormatProvider provider)
        {
            DateTimeFormatInfo dtfi = DateTimeFormatInfo.GetInstance(provider);

            string format = Regex.Replace(dtfi.ShortTimePattern, @"[^hHt\s]", "");
            format = Regex.Replace(format, @"\s+", " ").Trim();

            if (format.Length == 0)
                return "";

            if (format.Length == 1)
                format = '%' + format;

            return dt.ToString(format, dtfi);
        }
    }
}