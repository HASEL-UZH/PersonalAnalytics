// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-07
// 
// Licensed under the MIT License.

using System.ComponentModel;

namespace GoalSetting.Model
{
    /// <summary>
    /// Used in the Goal class as operator in the Rule Engine
    /// </summary>
    public enum RuleOperator
    {
        [Description("exactly")]
        Equal,

//        [Description("not")]
//        NotEqual,

        [Description("more than")]
        GreaterThan,

        [Description("less than")]
        LessThan,

//        [Description("more than or exactly")]
//        GreaterThanOrEqual,

//        [Description("less than or exactly")]
//        LessThanOrEqual
    }
}