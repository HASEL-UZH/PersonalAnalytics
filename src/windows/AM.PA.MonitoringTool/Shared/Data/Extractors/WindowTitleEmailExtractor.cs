// Created by André Meyer at MSR
// Created: 2016-01-26
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Shared.Data.Extractors
{
    /// <summary>
    /// This class offers some methods to receive information about a the time spent
    /// in Outlook. Attention: some information might be lost if the user uses Outlook
    /// only in the main window (e.g. writing emails not in new window)
    /// 
    /// Hint: The WindowsActivityTracker must be enabled to make use of the extractor
    /// </summary>
    public class WindowTitleEmailExtractor
    {
        public static List<ExtractedItem> GetTimeSpentInOutlook(DateTimeOffset date)
        {
            var emailInfos = new List<ExtractedItem>();

            try
            {
                var query = "SELECT window, sum(difference) / 60.0 as 'durInMin' "
                          + "FROM ( "
                          + "SELECT process, window, (strftime('%s', tsEnd) - strftime('%s', tsStart)) as 'difference' "
                          + "FROM " + Shared.Settings.WindowsActivityTable + " "
                          + "WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, "tsStart") + " and " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, "tsEnd") + " "
                          + "AND lower(process) = 'outlook' "
                          + "GROUP BY id, tsStart"
                          + ") "
                          + "WHERE difference > 0 "
                          + "GROUP BY window "
                          + "ORDER BY durInMin DESC;";

                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    var windowTitle = (string)row["window"];
                    var emailDetails = CleanWindowTitle(windowTitle);
                    var durInMin = (double)row["durInMin"];

                    if (string.IsNullOrEmpty(emailDetails) || durInMin < 1) continue;

                    var art = new ExtractedItem
                    {
                        ItemName = emailDetails,
                        DurationInMins = durInMin
                    };

                    emailInfos.Add(art);
                }
                table.Dispose();
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return emailInfos;
        }

        public static string CleanWindowTitle(string windowTitle)
        {
            foreach (var c in BaseRules.OutlookRules)
            {
                windowTitle = Regex.Replace(windowTitle, c, "").Trim();
            }

            return windowTitle;
        }
    }
}
