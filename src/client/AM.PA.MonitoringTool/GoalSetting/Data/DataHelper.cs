// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-27
// 
// Licensed under the MIT License.

using GoalSetting.Model;
using Shared.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoalSetting.Data
{
    public class DataHelper
    {
        public static List<ActivityContext> SetEndDateOfActivities(List<ActivityContext> activities, DateTime lastDate)
        {
            for (int i = 0; i < activities.Count; i++)
            {
                if (i + 1 < activities.Count)
                {
                    activities.ElementAt(i).End = activities.ElementAt(i+1).Start;
                }
                else
                {
                    activities.ElementAt(i).End = lastDate;
                }
            }

            return activities;
        }

        public static List<ActivityContext> MergeSameActivities(List<ActivityContext> activities, int minimumSwitchTimeInSeconds)
        {
            var result = new List<ActivityContext>();

            if (activities.Count <= 1)
            {
                return activities;
            }

            ActivityContext currentActivity = activities.First();
            DateTime currentStart = activities.First().Start;

            for (int i = 1; i < activities.Count; i++)
            {
                if (!activities.ElementAt(i).Equals(currentActivity))
                {
                    if (activities.ElementAt(i).Duration.TotalSeconds <= minimumSwitchTimeInSeconds)
                    {
                        //Ignore
                        continue;
                    }
                    else
                    {
                        result.Add(new ActivityContext { Activity = currentActivity.Activity, Start = currentStart, End = currentActivity.End });
                        currentStart = activities.ElementAt(i).Start;
                    }
                }
                currentActivity = activities.ElementAt(i);
            }

            result.Add(new ActivityContext { Activity = currentActivity.Activity, Start = currentStart, End = activities.Last().End });

            result = result.Where(a => a.Duration.TotalSeconds > minimumSwitchTimeInSeconds).ToList();

            return result;
        }

        public static int GetNumberOfSwitchesToActivity(List<ActivityContext> activities, ContextCategory activity)
        {
            return activities.Where(a => a.Activity.Equals(activity)).Count();
        }

        public static TimeSpan GetTotalTimeSpentOnActivity(List<ActivityContext> activities, ContextCategory activity)
        {
            double milliSeconds = activities.Where(a => a.Activity.Equals(activity)).Sum(a => a.Duration.TotalMilliseconds);
            return TimeSpan.FromMilliseconds(milliSeconds);
        }
    }

}