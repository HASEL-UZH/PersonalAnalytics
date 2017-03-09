// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-27
// 
// Licensed under the MIT License.

using GoalSetting.Model;
using System.Collections.Generic;

namespace GoalSetting
{
    public class Activity
    {
        public string Category { get; internal set; }
        public double TimeSpentOn { get; internal set; } //milliseconds. We can't use TimeSpan here since the rule engine wouldn't understand
        public int NumberOfSwitchesTo { get; internal set; }
        public List<ActivityContext> Context { get; internal set; }

        public string GetTimeSpentInHours()
        {
            return (TimeSpentOn / 1000 / 60 / 60).ToString("0.##");
        }

        public override string ToString()
        {
            return Category.ToString() + ": " + GetTimeSpentInHours()  + " hours / " + NumberOfSwitchesTo + " switches";
        }
    }
}