// Created by André Meyer at MSR
// Created: 2015-12-11
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace Shared.Data.Extractors
{
    /// <summary>
    /// This class offers some methods to receive code related information
    /// from a window title. Currently:
    /// 
    /// 1. Code Reviews (& time spent)
    /// 2. Time spent per Visual Studio project
    /// 
    /// Hint: The WIndowsActivityTracker must be enabled to make use of the extractor
    /// </summary>
    public class WindowTitleCodeExtractor
    {
        private static Dictionary<string, double> RunAndParseQuery(string query)
        {
            var items = new Dictionary<string, double>();

            try
            {
                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    var windowTitle = (string)row["window"];
                    var durInMin = (double)row["durInMin"];

                    if (string.IsNullOrEmpty(windowTitle)) continue;

                    if (items.ContainsKey(windowTitle))
                    {
                        items[windowTitle] += durInMin;
                    }
                    else
                    {
                        items.Add(windowTitle, durInMin);
                    }
                }
                table.Dispose();
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return items;
        }

        private static Dictionary<string, double> GetVisualStudioWindowTitles(DateTimeOffset date)
        {
            var query = "SELECT window, sum(difference) / 60.0 as 'durInMin' "
                          + "FROM ( "
                          + "SELECT process, window, (strftime('%s', tsEnd) - strftime('%s', tsStart)) as 'difference' "
                          + "FROM " + Shared.Settings.WindowsActivityTable + " "
                          + "WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, "tsStart") + " and " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, "tsEnd") + " "
                          + "AND (lower(process) = 'devenv' OR lower(process) = 'code') "
                          + "GROUP BY id, tsStart"
                          + ") "
                          + "WHERE difference > 0 "
                          + "GROUP BY window "
                          + "ORDER BY durInMin DESC;";

            var result = RunAndParseQuery(query);
            return result;
        }

        private static Dictionary<string, double> GetCodeFlowWindowTitles(DateTimeOffset date)
        {
            var query = "SELECT window, sum(difference) / 60.0 as 'durInMin' "
                        + "FROM ( "
                        + "SELECT process, window, (strftime('%s', tsEnd) - strftime('%s', tsStart)) as 'difference' "
                        + "FROM " + Shared.Settings.WindowsActivityTable + " "
                        + "WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, "tsStart") + " and " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, "tsEnd") + " "
                        + "AND lower(process) = 'codeflow' "
                        + "GROUP BY id, tsStart "
                        + ") "
                        + "WHERE difference > 0 "
                        + "GROUP BY window "
                        + "ORDER BY durInMin DESC;";

            var result = RunAndParseQuery(query);
            return result;
        }

        /// <summary>
        /// Returns a list of Code Reviews and how much time
        /// a user spent on them
        /// 
        /// todo: maybe in the future also look at code reviews done within VS
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static Dictionary<string, double> GetCodeReviewsDone(DateTimeOffset date)
        {
            var reviews = new Dictionary<string, double>();

            // handle reviews from CodeFlow
            var codeFlowItems = GetCodeFlowWindowTitles(date);
            foreach (var item in codeFlowItems)
            {
                // extract the project name from the window title
                var reviewName = GetReviewName(item.Key);

                // save project name
                if (string.IsNullOrEmpty(reviewName) || item.Value < 1) continue;
                if (reviews.ContainsKey(reviewName))
                {
                    reviews[reviewName] += item.Value;
                }
                else
                {
                    reviews.Add(reviewName, item.Value);
                }
            }

            // handle reviews from VS
            var vsItems = GetVisualStudioWindowTitles(date);
 
            foreach (var item in vsItems)
            {
                // extract the project name from the window title
                var reviewName = GetVsReviewName(item.Key);

                // save project name
                if (string.IsNullOrEmpty(reviewName) || item.Value < 1) continue;
                if (reviews.ContainsKey(reviewName))
                {
                    reviews[reviewName] += item.Value;
                }
                else
                {
                    reviews.Add(reviewName, item.Value);
                }
            }
            return reviews;
        }

        /// <summary>
        /// Returns a list of Visual Studio projects and how much time
        /// a user spent on them
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static Dictionary<string, double> GetVisualStudioProjects(DateTimeOffset date)
        {
            var vsItems = GetVisualStudioWindowTitles(date);
            var projects = new Dictionary<string, double>();

            foreach (var row in vsItems)
            {
                // use the window title to VS project name extractor
                var projectName = GetProjectName(row.Key);

                if (string.IsNullOrEmpty(projectName)) continue;
                if (projects.ContainsKey(projectName))
                {
                    projects[projectName] += row.Value;
                }
                else
                {
                    projects.Add(projectName, row.Value);
                }
            }

            return projects;
        }

        public static string GetProjectName(string windowTitle)
        {
            foreach (var c in BaseRules.VisualStudioRules)
            {
                windowTitle = Regex.Replace(windowTitle, c, "").Trim();
            }

            return windowTitle;
        }

        public static string GetReviewName(string windowTitle)
        {
            foreach (var c in BaseRules.CodeReviewRules)
            {
                windowTitle = windowTitle.Replace(c, "").Trim();
            }

            return windowTitle;
        }

        public static string GetVsReviewName(string windowTitle)
        {
            foreach (var c in BaseRules.VisualStudioCodeReviewRules)
            {
                if (windowTitle.Contains(c))
                {
                    windowTitle = windowTitle.Replace(c, "").Trim();
                    return windowTitle;
                }
            }

            return "";
        }
    }
}
