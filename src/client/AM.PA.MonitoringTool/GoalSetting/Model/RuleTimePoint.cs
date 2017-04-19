// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

using System.ComponentModel;

namespace GoalSetting.Model
{
    public enum RuleTimePoint
    {
        [Description("At the start of the day")]
        Start,

        [Description("At the end of the day")]
        End,

        [Description("At")]
        Timepoint,
    }
}
