// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-04-03
// 
// Licensed under the MIT License.

using Shared.Helpers;
using GoalSetting.Model;
using GoalSetting.Rules;
using System;

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
            double actual = DatabaseConnector.GetLatestEmailInboxCount();
            double target = Double.Parse(this.Rule.TargetValue);
            double percentage = actual / target;

            if (actual == target)
            {
                Progress.Status = ProgressStatus.VeryHigh;
                Progress.Success = true;
            }
            else
            {
                Progress.Success = false;
                
                if (percentage >= 0.75 && percentage <= 1.25)
                {
                    Progress.Status = ProgressStatus.High;
                }
                else if (percentage >= 0.5 && percentage <= 1.5)
                {
                    Progress.Status = ProgressStatus.Average;
                }
                else if (percentage >= 0.25 && percentage <= 1.75)
                {
                    Progress.Status = ProgressStatus.Low;
                }
                else
                {
                    Progress.Status = ProgressStatus.VeryLow;
                }
            }
        }

        public override string GetProgressMessage()
        {
            var inbox = DatabaseConnector.GetLatestEmailInboxCount();
            double target = Double.Parse(this.Rule.TargetValue);

            if (inbox == target)
            {
                return "You have " + inbox + " emails right now. You have reached your goal!";
            }

            return "You have " + inbox + " emails right now. You should have " + Math.Abs(inbox - target) + (inbox > target ? " less " : " more ") + " emails.";
        }
    }
}