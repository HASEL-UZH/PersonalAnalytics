// Created by André Meyer at MSR
// Created: 2015-12-04
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace Shared.Data.Extractors
{
    /// <summary>
    /// This class offers some methods to receive detailed information about a website,
    /// extravcted from the rpgram, tracked by the WindowsActivityTracker
    /// 
    /// Hint: The WIndowsActivityTracker must be enabled to make use of the extractor
    /// </summary>
    public class WindowTitleWebsitesExtractor
    {
        #region Query Database for Window Titles

        public static List<ExtractedItem> GetWebsitesVisited(DateTimeOffset date)
        {
            var files = new List<ExtractedItem>();

            try
            {
                var onlySearchForProgramsWhereRulesExists = GetSqlForProgramsToSearch();

                var query = "SELECT process, window, sum(difference) / 60.0 as 'durInMin' "
                          + "FROM ( "
                          + "SELECT t1.id, t1.process, t1.window, t1.time as 'from', t2.time as 'to', (strftime('%s', t2.time) - strftime('%s', t1.time)) as 'difference' "
                          + "FROM " + Shared.Settings.WindowsActivityTable + " t1 LEFT JOIN " + Shared.Settings.WindowsActivityTable + " t2 on t1.id + 1 = t2.id "
                          + "WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, "t1.time") + " and " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, "t2.time") + " "
                          + onlySearchForProgramsWhereRulesExists
                          + "GROUP BY t1.id, t1.time "
                          + ") "
                          + "WHERE difference > 0 "
                          + "GROUP BY window "
                          + "ORDER BY durInMin DESC;";

                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    var process = (string)row["process"];
                    var windowTitle = (string)row["window"];
                    var website = GetWebsiteDetails(process, windowTitle);
                    var durInMin = (double)row["durInMin"];

                    if (string.IsNullOrEmpty(website) || durInMin < 1) continue;

                    var art = new ExtractedItem
                    {
                        ItemName = website,
                        DurationInMins = durInMin
                    };

                    files.Add(art);
                }
                table.Dispose();
            }
            catch (Exception e)
            {
                Shared.Logger.WriteToLogFile(e);
            }

            return files;
        }

        private static string GetSqlForProgramsToSearch()
        {
            var websiteRules = BaseRules.WebsiteRules;
            var onlySearchForProgramsWhereRulesExists = string.Empty;
            if (websiteRules.Count > 0)
            {
                onlySearchForProgramsWhereRulesExists += "AND (";
                for (var i = 0; i < websiteRules.Count; i++)
                {
                    var processName = websiteRules[i].ProcessName.ToLower();

                    // special case for Microsoft Edge (as its process name might be applicationframehost, which is the same for the Photos, Calculator, Settings, etc. apps)
                    if (processName == "applicationframehost")
                    {
                        onlySearchForProgramsWhereRulesExists += "(lower(t1.process) = '" + processName + "' and lower(t1.window) LIKE '%Microsoft Edge%') ";
                    }
                    else
                    {
                        onlySearchForProgramsWhereRulesExists += "lower(t1.process) = '" + processName + "' ";
                    }

                    if (i + 1 < websiteRules.Count) onlySearchForProgramsWhereRulesExists += "or ";
                }
                onlySearchForProgramsWhereRulesExists += ") ";
            }

            return onlySearchForProgramsWhereRulesExists;
        }


        #endregion

        /// <summary>
        /// To get the website name, we do the following:
        /// 1. change the process name if it is just applicationhost (Windows 10 apps)
        /// 2. clean the window title
        /// </summary>
        /// <param name="process"></param>
        /// <param name="windowTitle"></param>
        /// <returns></returns>
        public static string GetWebsiteDetails(string process, string windowTitle)
        {
            process = BaseRules.RunApplicationHostTitleCleaning(process, windowTitle);
            windowTitle = CleanWindowTitle(process, windowTitle);
            //if (fileName.Length == 0) return string.Empty; // could not get filename
            //var fileNameWithExtension = ValidateOrAddExtension(fileName, process);

            return windowTitle;
        }

        private static string CleanWindowTitle(string process, string websiteTitle)
        {
            var removables = BaseRules.GetRemovablesFromProcess(process, BaseRules.WebsiteRules);

            // remove programname from title 
            if (removables != null && removables.Count > 0)
            {
                foreach (var r in removables)
                {
                    websiteTitle = Regex.Replace(websiteTitle, r, "").Trim();
                }
            }
            return websiteTitle.Trim();
        }
    }
}
