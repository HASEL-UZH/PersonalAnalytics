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
        /// <summary>
        /// Expects a list of activities. Sets the end date of each activity to the start date of the next activity. For the last activity, the end date is set to the date passed in the second parameter.
        /// </summary>
        /// <param name="activities"></param>
        /// <param name="lastDate"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Expects a list of activities and merges subsequent activities if the second activity is the same as the first one. Activities a user has spent less
        /// than the amount of seconds passed in the second parameters are ignored.
        /// </summary>
        /// <param name="activities"></param>
        /// <param name="minimumSwitchTimeInSeconds"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Expects a list of activities and a specific activity. Returns how many times a user has switched to the specific activity.
        /// </summary>
        /// <param name="activities"></param>
        /// <param name="activity"></param>
        /// <returns></returns>
        public static int GetNumberOfSwitchesToActivity(List<ActivityContext> activities, ContextCategory activity)
        {
            return activities.Where(a => a.Activity.Equals(activity)).Count();
        }

        /// <summary>
        /// Expectes a list of activities and a specific activity. Returns how much time a user has spent on the specific activity.
        /// </summary>
        /// <param name="activities"></param>
        /// <param name="activity"></param>
        /// <returns></returns>
        public static TimeSpan GetTotalTimeSpentOnActivity(List<ActivityContext> activities, ContextCategory activity)
        {
            double milliSeconds = activities.Where(a => a.Activity.Equals(activity)).Sum(a => a.Duration.TotalMilliseconds);
            return TimeSpan.FromMilliseconds(milliSeconds);
        }

        internal static bool SuccessRule(Rule rule, double value)
        {
            var target = double.Parse(rule.TargetValue);
            switch (rule.Operator)
            {
                case RuleOperator.Equal:
                    return value == target;

                case RuleOperator.NotEqual:
                    return value != target;

                case RuleOperator.GreaterThan:
                    return value > target;

                case RuleOperator.GreaterThanOrEqual:
                    return value >= target;

                case RuleOperator.LessThan:
                    return value < target;

                case RuleOperator.LessThanOrEqual:
                    return value <= target;

                default:
                    throw new ArgumentException(rule.Operator + " not known!");
            }
        }
    }

}