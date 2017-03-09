// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

using System.ComponentModel;

namespace GoalSetting.Rules
{
    public enum RuleTimeSpan
    {
        [Description("day")]
        EveryDay,

        [Description("Monday")]
        Monday,

        [Description("Tuesday")]
        Tuesday,

        [Description("Wednesday")]
        Wednesday,

        [Description("Thursday")]
        Thursday,

        [Description("Friday")]
        Friday,

        [Description("Saturday")]
        Saturday,

        [Description("Sunday")]
        Sunday,

        [Description("week")]
        Week,

        [Description("month")]
        Month,

        [Description("morning")]
        Morning,

        [Description("afternoon")]
        Afternoon
    }
}