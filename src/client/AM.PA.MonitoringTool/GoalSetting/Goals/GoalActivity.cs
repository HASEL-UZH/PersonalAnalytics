// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-04-03
// 
// Licensed under the MIT License.

using GoalSetting.Model;
using GoalSetting.Rules;
using Shared.Data;
using Shared.Helpers;
using System;
using System.Linq;

namespace GoalSetting.Goals
{
    public class GoalActivity : Goal
    {

        public ContextCategory Activity { get; set; }

        private RuleTimeSpan? _timeSpan;
        public RuleTimeSpan? TimeSpan { get { return _timeSpan; } set { _timeSpan = value; base.When = _timeSpan.ToString(); } }

        public override string ToString()
        {
            string str = string.Empty;

            switch (Rule.Goal)
            {
                case RuleGoal.NumberOfSwitchesTo:
                    str += "I want to switch ";
                    str += FormatStringHelper.GetDescription(Rule.Operator).ToLower() + " ";
                    str += Rule.TargetValue + " ";
                    str += "times to ";
                    str += FormatStringHelper.GetDescription(Activity) + " ";
                    str += "per " + FormatStringHelper.GetDescription(TimeSpan) + ".";
                    break;

                case RuleGoal.TimeSpentOn:
                    str += "I want to spend ";
                    str += FormatStringHelper.GetDescription(Rule.Operator).ToLower() + " ";
                    str += Rule.TargetTimeSpan;
                    str += " on " + FormatStringHelper.GetDescription(Activity) + " ";
                    str += "per " + FormatStringHelper.GetDescription(TimeSpan) + ".";
                    break;
            }
            return str;
        }

        public override void Compile()
        {
            CompiledRule = RuleEngine.CompileRule<Activity>(Rule);
        }

        public override string GetProgressMessage()
        {
            return (string.IsNullOrEmpty(Progress.Time) ? "0" : Progress.Time) + " hours / " + Progress.Switches + " switches";
        }

        public override void CalculateProgressStatus(bool persist)
        {
            var activities = GoalSettingManager.Instance.GetActivitiesPerTimeSpan(this.TimeSpan.Value);
            var activity = activities.Where(a => a.Category.Equals(this.Activity.ToString())).First();

            if (activity != null)
            {
                this.Compile();
                this.Progress.Success = this.CompiledRule(activity);
                this.Progress.Time = activity.GetTimeSpentInHours();
                this.Progress.Switches = activity.NumberOfSwitchesTo;
            }

            double target = Double.NaN;
            double actual = Double.NaN;

            switch (Rule.Goal)
            {
                case RuleGoal.TimeSpentOn:
                    target = Double.Parse(Rule.TargetValue) / 1000 / 60 / 60;
                    actual = string.IsNullOrEmpty(Progress.Time) ? 0.0 : Double.Parse(Progress.Time);
                    break;
                case RuleGoal.NumberOfSwitchesTo:
                    target = Double.Parse(Rule.TargetValue);
                    actual = Progress.Switches;
                    break;
            }

            Progress.Actual = actual;
            Progress.Target = target;
            double percentage = actual / target;

            if (Rule.Operator == RuleOperator.GreaterThan || Rule.Operator == RuleOperator.GreaterThanOrEqual)
            {
                if (percentage < 0.3)
                {
                    Progress.Status = ProgressStatus.VeryLow;
                }
                else if (percentage < 0.7)
                {
                    Progress.Status = ProgressStatus.Low;
                }
                else if (percentage < 0.9)
                {
                    Progress.Status = ProgressStatus.Average;
                }
                else if (percentage < 1)
                {
                    Progress.Status = ProgressStatus.High;
                }
                else
                {
                    Progress.Status = ProgressStatus.VeryHigh;
                }

            }
            else if (Rule.Operator == RuleOperator.LessThan || Rule.Operator == RuleOperator.LessThanOrEqual)
            {
                if (percentage < 0.9)
                {
                    Progress.Status = ProgressStatus.VeryHigh;
                }
                else if (percentage <= 1)
                {
                    Progress.Status = ProgressStatus.High;
                }
                else if (percentage <= 1.1)
                {
                    Progress.Status = ProgressStatus.Average;
                }
                else if (percentage <= 1.5)
                {
                    Progress.Status = ProgressStatus.Low;
                }
                else
                {
                    Progress.Status = ProgressStatus.VeryLow;
                }
            }
            else
            {
                if (Progress.Success.HasValue && Progress.Success.Value)
                {
                    Progress.Status = ProgressStatus.VeryHigh;
                }
                else if (percentage <= 1.1 && percentage >= 0.9)
                {
                    Progress.Status = ProgressStatus.High;
                }
                else if (percentage <= 1.2 && percentage >= 0.8)
                {
                    Progress.Status = ProgressStatus.Average;
                }
                else if (percentage <= 1.3 && percentage >= 0.7)
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
                if (persist)
                {
                    DatabaseConnector.SaveAchievement(this, DateTime.Now);
                }
            }
        }
    }
}