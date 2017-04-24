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
        /// <summary>
        /// String representation of the activity
        /// </summary>
        public string Category { get; internal set; }

        /// <summary>
        /// Time spent on this activity in milliseconds
        /// </summary>
        public double TimeSpentOn { get; internal set; } //milliseconds. We can't use TimeSpan here since the rule engine wouldn't understand

        /// <summary>
        /// Number of switches to this activity
        /// </summary>
        public int NumberOfSwitchesTo { get; internal set; }

        /// <summary>
        /// List of all the times the user has spent on this activity
        /// </summary>
        public List<ActivityContext> Context { get; internal set; }

        /// <summary>
        /// Return the amount of time the user has spent on this activity
        /// </summary>
        /// <returns></returns>
        public string GetTimeSpentInHours()
        {
            return (TimeSpentOn / 1000 / 60 / 60).ToString("0.##");
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Category.ToString() + ": " + GetTimeSpentInHours()  + " hours / " + NumberOfSwitchesTo + " switches";
        }
    }
}