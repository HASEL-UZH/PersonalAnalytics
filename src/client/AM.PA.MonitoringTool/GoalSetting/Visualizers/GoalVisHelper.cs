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
        public static string getDataPointName(PARule _rule, VisType type)
        {
            switch (_rule.Rule.Goal)
            {
                case Goal.NumberOfSwitchesTo:
                    return "d.switch";
                case Goal.TimeSpentOn:
                    return "d.time";
                case Goal.NumberOfEmailsInInbox:
                    return "d.mail";
                default:
                    throw new ArgumentException(_rule.Rule.Goal + " is not a valid goal.");
            }
        }

        public static string getXAxisTitle(PARule _rule, VisType type)
        {
            switch (_rule.Rule.Goal)
            {
                case Goal.NumberOfSwitchesTo:
                    return "# Switches";
                case Goal.TimeSpentOn:
                    return "Time spent";
                case Goal.NumberOfEmailsInInbox:
                    return "# Emails";
                default:
                    throw new ArgumentException(_rule.Rule.Goal + " is not a valid goal.");
            }
        }

        public static string getHintText(PARule _rule, VisType type)
        {
            switch (_rule.Rule.Goal)
            {
                case Goal.NumberOfSwitchesTo:
                    return type == VisType.Day ? "Number of switches during today" : "Number of switches per day";
                case Goal.TimeSpentOn:
                    return type == VisType.Day ? "Time spent on " + FormatStringHelper.GetDescription(_rule.Activity) : "Time spent on " + FormatStringHelper.GetDescription(_rule.Activity) + " per day";
                case Goal.NumberOfEmailsInInbox:
                    return type == VisType.Day ? "Number of emails in inbox during the day" : "Number of emails in the inbox at " + _rule.Time + " for each day";
                default:
                    throw new ArgumentException(_rule.Rule.Goal + " is not a valid goal.");
            }
        }

    }
}