// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

using Shared.Data;

namespace GoalSetting.Rules
{
    public class PARule
    {

        public Rule Rule { get; set; }

        public ContextCategory Activity { get; set; }

        public RuleTimeSpan TimeSpan { get; set; }

        public override string ToString()
        {
            return Rule.Goal + " " + Activity.ToString() + " " + Rule.Operator.ToString() + " " + Rule.TargetValue.ToString() + " (per " + TimeSpan.ToString() + ")";
        }

    }

}