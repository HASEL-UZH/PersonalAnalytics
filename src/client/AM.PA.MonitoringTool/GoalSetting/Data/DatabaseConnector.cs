// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

using GoalSetting.Data;
using GoalSetting.Model;
using Shared;
using Shared.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace GoalSetting
{
    class DatabaseConnector
    {

        #region SELECT

        public static List<ActivityContext> GetActivitiesSince(DateTime date)
        {
            var activities = new List<ActivityContext>();

            var query = "SELECT * FROM " + Settings.ActivityTable + " WHERE Time > '" + date.ToString(Settings.DateFormat) + "';";
            var table = Database.GetInstance().ExecuteReadQuery(query);

            try
            {
                if (table != null && table.Rows.Count > 0)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        var dto = new ContextDto { Context = new ContextInfos { ProgramInUse = row["process"].ToString(), WindowTitle = row["window"].ToString() } };
                        activities.Add(new ActivityContext { Activity = ContextMapper.GetContextCategory(dto), Start = DateTime.ParseExact(row["time"].ToString(), Settings.DateFormat, CultureInfo.InvariantCulture) });
                    }
                }
                else
                {
                    table.Dispose();
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
            finally
            {
                table.Dispose();
            }

            activities = DataHelper.SetEndDateOfActivities(activities);

            return activities;
        }
        
        #endregion

    }
}