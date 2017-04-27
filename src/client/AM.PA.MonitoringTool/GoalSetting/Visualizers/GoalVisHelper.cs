// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-04-03
// 
// Licensed under the MIT License.

using GoalSetting.Goals;
using GoalSetting.Model;
using Shared;
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