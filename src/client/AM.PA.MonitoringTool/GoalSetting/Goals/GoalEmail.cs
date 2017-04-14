// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-04-03
// 
// Licensed under the MIT License.

using Shared.Helpers;
using GoalSetting.Model;
using GoalSetting.Rules;

namespace GoalSetting.Goals
{
    public class GoalEmail : Goal
    {
        private RuleTimePoint? _timePoint;
        public RuleTimePoint? TimePoint { get { return _timePoint; } set { _timePoint = value; base.When = _timePoint.ToString(); } }

        private string _time;
        public string Time { get { return _time; } set { _time = value; if (!_timePoint.HasValue) { base.When = _time; } } }

        public override string ToString()
        {
            string str = string.Empty;

            str += "The number of emails in my inbox ";
            str += "should be ";
            str += FormatStringHelper.GetDescription(Rule.Operator).ToLower() + " ";
            str += Rule.TargetValue + " ";
            if (TimePoint == RuleTimePoint.Timepoint)
            {
                str += "at " + Time + ".";
            }
            else
            {
                str += FormatStringHelper.GetDescription(TimePoint).ToLower() + ".";
            }
                    
            return str;
        }

        public override void Compile()
        {
            CompiledRule = RuleEngine.CompileRule<Activity>(Rule);
        }

        public override void CalculateProgressStatus()
        {
           //TODO
        }

        public override string GetProgressMessage()
        {
            return "Not yet supported";
        }
    }
}
