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
        public static string getDataPointName(GoalActivity goal, VisType type)
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

        public static string getXAxisTitle(GoalActivity goal, VisType type)
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

        public static string getHintText(GoalActivity goal, VisType type)
        {
            switch (goal.Rule.Goal)
            {
                case RuleGoal.NumberOfSwitchesTo:
                    return type == VisType.Day ? "Number of switches during today" : "Number of switches per day";
                case RuleGoal.TimeSpentOn:
                    return type == VisType.Day ? "Time spent on " + FormatStringHelper.GetDescription(goal.Activity) : "Time spent on " + FormatStringHelper.GetDescription(goal.Activity) + " per day";
                default:
                    throw new ArgumentException(goal.Rule.Goal + " is not a valid goal.");
            }
        }

        internal static string getLimitValue(GoalActivity goal, VisType day)
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