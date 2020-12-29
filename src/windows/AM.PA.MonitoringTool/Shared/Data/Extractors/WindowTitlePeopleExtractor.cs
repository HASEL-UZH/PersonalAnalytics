// Created by André Meyer at MSR
// Created: 2015-12-10
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Data;

namespace Shared.Data.Extractors
{
    /// <summary>
    /// This class offers methods to receive a list of people
    /// a user was in contact with via Skype for Business
    /// </summary>
    public static class WindowTitlePeopleExtractor
    {
        /*
        public static List<ContactItem> GetPeopleInContact(DateTimeOffset date)
        {
            var contacts = new List<ContactItem>();

            try
            {
                var excludeSomeWindowTitles = GetSqlForWindowTitlesToExclude();

                var query = "SELECT window, count(*) as 'interactionCount'"
                          + "FROM " + Shared.Settings.WindowsActivityTable + " "
                          + "WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date) + " "
                          + "AND process = 'lync' "
                          + excludeSomeWindowTitles
                          + "GROUP BY window "
                          + "ORDER BY interactionCount DESC;";

                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    var contactName = (string)row["window"];
                    var interactionCount = Convert.ToInt32(row["interactionCount"]);

                    if (string.IsNullOrEmpty(contactName) || interactionCount == 0) continue;

                    var contact = new ContactItem(contactName, "", interactionCount);
                    contacts.Add(contact);
                }
                table.Dispose();
            }
            catch (Exception e)
            {
                Shared.Logger.WriteToLogFile(e);
            }

            return contacts;
        }
       

        private static string GetSqlForWindowTitlesToExclude()
        {
            var peopleRules = BaseRules.PeopleRules;
            var excludeWindowTitles = string.Empty;
            if (peopleRules.Count > 0)
            {
                excludeWindowTitles += "AND (";
                for (var i = 0; i < peopleRules.Count; i++)
                {
                    excludeWindowTitles += "lower(window) NOT LIKE '%" + peopleRules[i] + "%' ";
                    if (i + 1 < peopleRules.Count) excludeWindowTitles += "AND ";
                }
                excludeWindowTitles += ") ";
            }

            return excludeWindowTitles;
        }
         */
    }
}
