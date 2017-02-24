// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.


using Shared.Data;

namespace GoalSetting.Model
{
    public class Rule
    {

        public Goal Goal { get; set; }

        public string Operator { get; set; }

        public int Value { get; set; }

        public ContextCategory Activity { get; set; }

        public RuleTimeSpan TimeSpan { get; set; }

        public override string ToString()
        {
            return Goal.ToString() + " " + Activity.ToString() + " " + Operator.ToString() + " " + Value.ToString() + " (per " + TimeSpan.ToString() + ")";
        }

    }

}