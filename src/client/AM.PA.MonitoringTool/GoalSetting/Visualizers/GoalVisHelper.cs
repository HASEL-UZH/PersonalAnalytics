// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-04-03
// 
// Licensed under the MIT License.

using GoalSetting.Goals;
using GoalSetting.Model;
using Shared;
using Shared.Helpers;
using System;

namespace GoalSetting.Visualizers
{
    public class GoalVisHelper
    {
        public static string GetDataPointName(GoalActivity goal, VisType type)
        {
            switch (goal.Rule.Goal)
            {
                case RuleGoal.NumberOfSwitchesTo:
                    return "d.switch";
                case RuleGoal.TimeSpentOn:
                    return "d.time";
                default:
                    throw new ArgumentException(goal.Rule.Goal + " is not a valid goal.");
            }
        }

        public static string GetXAxisTitle(GoalActivity goal, VisType type)
        {
            switch (goal.Rule.Goal)
            {
                case RuleGoal.NumberOfSwitchesTo:
                    return "# Switches";
                case RuleGoal.TimeSpentOn:
                    return "Time spent";
                default:
                    throw new ArgumentException(goal.Rule.Goal + " is not a valid goal.");
            }
        }

        public static string GetHintText(GoalActivity goal, VisType type)
        {
            if (goal.Progress.Success.HasValue && goal.Progress.Success.Value)
            {
                switch (goal.Rule.Goal)
                {
                    case RuleGoal.NumberOfSwitchesTo:
                        return "Congratulations, you reached your goal! You switched " + goal.Progress.Actual + " to this activity.";
                    case RuleGoal.TimeSpentOn:
                        return "Congratulations, you reached your goal! You spent " + goal.Progress.Actual + " hours on this activity.";
                    case RuleGoal.NumberOfEmailsInInbox:
                        return "Congratulations, you reached your goal! You have " + goal.Progress.Actual + " emails in your inbox.";
                    default:
                        throw new ArgumentException(goal + " is not recognized!");
                }
            }
            else if (goal.IsStillReachable())
            {
                switch (goal.Rule.Goal)
                {
                    case RuleGoal.NumberOfSwitchesTo:
                        return "You have not yet reached your goal. However, you cann still reach it. You switched to this activity " + goal.Progress.Actual + " of " + goal.Progress.Target + " times.";
                    case RuleGoal.TimeSpentOn:
                        return "You have not yet reached your goal. However, you can still reach it. You spent " + goal.Progress.Actual + " of " + goal.Progress.Target + " hours and this activity.";
                    case RuleGoal.NumberOfEmailsInInbox:
                        double difference = goal.Progress.Target - goal.Progress.Actual;
                        return "You have not yet reached your goal. However, you can still reach it. You need " + difference + (difference < 0 ? "less" : "more") + " emails in your inbox.";
                    default:
                        throw new ArgumentException(goal + " is not recognized!");
                }
            }
            else
            {
                return "Unfortunately, you missed your goal this time.";
            }
        }

        internal static string GetLimitValue(GoalActivity goal, VisType day)
        {
            if (goal.Rule.Goal == RuleGoal.NumberOfSwitchesTo)
            {
                return goal.Rule.TargetValue;
            }
            else
            {
                return TimeSpan.FromMilliseconds(double.Parse(goal.Rule.TargetValue)).TotalMinutes.ToString();
            }
        }
    }
}