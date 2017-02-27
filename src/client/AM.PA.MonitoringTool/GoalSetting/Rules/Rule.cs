// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-27
// 
// Licensed under the MIT License.

namespace GoalSetting.Rules
{
    public class Rule
    {
        public string Goal { get; set; }

        public string Operator { get; set; }
        
        public string TargetValue { get; set; }
    }
}