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
        
        internal static string GetLimitValue(GoalActivity goal, VisType visType)
        {
            if (visType == VisType.Day)
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
            else if (visType == VisType.Week)
            {
                if (goal.Rule.Goal == RuleGoal.NumberOfSwitchesTo)
                {
                    return goal.Rule.TargetValue;
                }
                else
                {
                    return TimeSpan.FromMilliseconds(double.Parse(goal.Rule.TargetValue)).TotalHours.ToString();
                }
            }
            else
            {
                return "0";
            }
        }

        public static string GetVeryHighColor()
        {
            return "#39B54A";
        }

        public static string GetHighColor()
        {
            return "#B9D11F";
        }

        public static string GetAverageColor()
        {
            return "#A3A3A3";
        }

        public static string GetLowColor()
        {
            return "#F7931E";
        }

        public static string GetVeryLowColor()
        {
            return "#C1272D";
        }
    }
}