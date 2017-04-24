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
        /// <summary>
        /// Each GoalEmail is checked at a specific point in time, e.g. at the end of the workday or at a specific time. This property stores this point in time.
        /// </summary>
        public RuleTimePoint? TimePoint { get { return _timePoint; } set { _timePoint = value; base.When = _timePoint.ToString(); } }

        private string _time;
        /// <summary>
        /// A string representation of the 'TimePoint' property.
        /// </summary>
        public string Time { get { return _time; } set { _time = value; if (!_timePoint.HasValue) { base.When = _time; } } }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        /// <returns></returns>
        public override void Compile()
        {
            CompiledRule = RuleEngine.CompileRule<Activity>(Rule);
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        /// <returns></returns>
        public override void CalculateProgressStatus(bool persist)
        {
            double actual = DatabaseConnector.GetLatestEmailInboxCount();
            double target = Double.Parse(this.Rule.TargetValue);
            Progress.Actual = actual;
            Progress.Target = target;
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

            if (persist)
            {
                DatabaseConnector.SaveAchievement(this, DateTime.Now);
            }
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        /// <returns></returns>
        public override bool IsStillReachable()
        {
            return true;
        }
    }
}