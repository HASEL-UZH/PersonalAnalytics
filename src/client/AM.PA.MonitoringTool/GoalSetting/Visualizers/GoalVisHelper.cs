// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-04-03
// 
// Licensed under the MIT License.

using GoalSetting.Model;
using GoalSetting.Rules;
using Shared;
using Shared.Helpers;
using System;

namespace GoalSetting.Visualizers
{
    public class GoalVisHelper
    {
        public static string getDataPointName(PARuleActivity _rule, VisType type)
        {
            switch (_rule.Rule.Goal)
            {
                case Goal.NumberOfSwitchesTo:
                    return "d.switch";
                case Goal.TimeSpentOn:
                    return "d.time";
                default:
                    throw new ArgumentException(_rule.Rule.Goal + " is not a valid goal.");
            }
        }

        public static string getXAxisTitle(PARuleActivity _rule, VisType type)
        {
            switch (_rule.Rule.Goal)
            {
                case Goal.NumberOfSwitchesTo:
                    return "# Switches";
                case Goal.TimeSpentOn:
                    return "Time spent";
                default:
                    throw new ArgumentException(_rule.Rule.Goal + " is not a valid goal.");
            }
        }

        public static string getHintText(PARuleActivity _rule, VisType type)
        {
            switch (_rule.Rule.Goal)
            {
                case Goal.NumberOfSwitchesTo:
                    return type == VisType.Day ? "Number of switches during today" : "Number of switches per day";
                case Goal.TimeSpentOn:
                    return type == VisType.Day ? "Time spent on " + FormatStringHelper.GetDescription(_rule.Activity) : "Time spent on " + FormatStringHelper.GetDescription(_rule.Activity) + " per day";
                default:
                    throw new ArgumentException(_rule.Rule.Goal + " is not a valid goal.");
            }
        }

    }
}