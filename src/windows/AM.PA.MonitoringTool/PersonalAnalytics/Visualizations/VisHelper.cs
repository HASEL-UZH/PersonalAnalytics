// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using System;
using Shared;

namespace PersonalAnalytics.Visualizations
{
    static class VisHelper
    {
        /// <summary>
        /// Returns a message that says that there is not enough data to
        /// provide a visualization and a standard message if no other is specified.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string NotEnoughData(string message = "We don't yet have enough data to show you a retrospection of your workday.")
        {
            return "<br/><br/><div align=\"center\">" + message + "</div>";
        }

        /// <summary>
        /// Returns a formated chart title with a specified title
        /// or a default one if there is none.
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public static string ChartTitle(string title = "Visualization")
        {
            return "<h3 style='text-align: center;'>" + title + "</h3>";
        }

        /// <summary>
        /// Parses the request parameter (for "date") as a DateTime
        /// or returns a fallback value if there is no or if it fails.
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static DateTimeOffset GetVisualizationDateFromUrlParameters(HttpReqResp req)
        {
            var fallbackDate = DateTimeOffset.Now;
            try
            {
                var dateParam = req["date"];
                if (dateParam != null)
                {
                    return DateTimeOffset.Parse(dateParam);
                }
            }
            catch { }

            return fallbackDate;
        }

        public static string GetVisualizationTypesFromUrlParameters(HttpReqResp req)
        {
            return req["vis"];
        }
    }
}
