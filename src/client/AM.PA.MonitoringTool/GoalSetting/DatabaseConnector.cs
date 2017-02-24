// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

using GoalSetting.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoalSetting
{
    class DatabaseConnector
    {

        #region SELECT

        public static List<ActivityContext> GetActivitiesSince(DateTime date)
        {
            var activities = new List<ActivityContext>();
            //            Select* from windows_activity where time > '2017-02-24 13:00:00:00'

            return activities;
        }


        #endregion

    }
}
