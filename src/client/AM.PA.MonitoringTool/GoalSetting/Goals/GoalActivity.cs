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
        /// <summary>
        /// Each GoalActivity is associated with a specific activity that is stored in this property
        /// </summary>
        public ContextCategory Activity { get; set; }

        private RuleTimeSpan? _timeSpan;
        /// <summary>
        /// Each GoalActivity can be defined for various time spans, e.g. hours, days, weeks or months. This property stores this time span. 
        /// </summary>
        public RuleTimeSpan? TimeSpan { get { return _timeSpan; } set { _timeSpan = value; base.When = _timeSpan.ToString(); } }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        /// <returns></returns>
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
        public override string GetProgressMessage()
        {
            CalculateProgressStatus(false);

            if (Progress.Success.HasValue && Progress.Success.Value)
            {
                if (Rule.Goal == RuleGoal.NumberOfSwitchesTo)
                {
                    return "You're on a good track to reach your goal! You switched " + Progress.Actual + " times to this activity so far.";
                }
                else if (Rule.Goal == RuleGoal.TimeSpentOn)
                {
                    return "You're on a good track to reach your goal! You spent " + Progress.Actual + " hours on this activity so far.";
                }
            }
            else if (IsStillReachable())
            {
                if (Rule.Goal == RuleGoal.NumberOfSwitchesTo)
                {
                    return "You have not yet reached your goal. However, you can still reach it. You switched to this activity " + Progress.Actual + " of " + Progress.Target + " times.";
                }
                else if (Rule.Goal == RuleGoal.TimeSpentOn)
                {
                    return "You have not yet reached your goal. However, you can still reach it. You spent " + Progress.Actual + " of " + Progress.Target + " hours and this activity.";
                }
            }
            else
            {
                if (Rule.Goal == RuleGoal.NumberOfSwitchesTo)
                {
                    switch (Rule.Operator)
                    {
                        case RuleOperator.Equal:
                            return "Unfortunately, you missed your goal this time. You switched " + Progress.Actual + " times while you wanted to switch " + Progress.Target + " times.";

                        case RuleOperator.GreaterThan:
                            return "Unfortunately, you missed your goal this time. You switched " + (Progress.Target - Progress.Actual) + " (+" + ( (Progress.Target - Progress.Actual) / Progress.Target * 100).ToString("N0") + "%) less than your goal.";

                        case RuleOperator.LessThan:
                            return "Unfortunately, you missed your goal this time. You switched " + (Progress.Actual - Progress.Target) + " (+" + (Progress.Actual / Progress.Target * 100).ToString("N0") + "%) more than your goal.";
                    }
                }
                else if (Rule.Goal == RuleGoal.TimeSpentOn)
                {
                    switch (Rule.Operator)
                    {
                        case RuleOperator.Equal:
                            return "Unfortunately, you missed your goal this time. You spent " + Progress.Actual.ToString("N2") + " hours on this activity while you wanted to spend " + Progress.Target.ToString("N2") + " hours.";

                        case RuleOperator.GreaterThan:
                            return "Unfortunately, you missed your goal this time. You spent " + (Progress.Target - Progress.Actual).ToString("N2") + " (+" + ( (Progress.Target - Progress.Actual) / Progress.Target * 100).ToString("N0") + "%) hours less than your goal on this activity.";

                        case RuleOperator.LessThan:
                            return "Unfortunately, you missed your goal this time. You spent " + (Progress.Actual - Progress.Target).ToString("N2") + " (+" + (Progress.Actual / Progress.Target * 100).ToString("N0") + "%) hours more than your goal on this activity.";
                    }
                }
            }
            return "Unknown progress towards this goal";
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        /// <returns></returns>
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
                    target = System.TimeSpan.FromMilliseconds(Double.Parse(Rule.TargetValue)).TotalHours;
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

            if (Rule.Operator == RuleOperator.GreaterThan)
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
            else if (Rule.Operator == RuleOperator.LessThan)
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

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        /// <returns></returns>
        public override bool IsStillReachable()
        {
            switch (Rule.Operator)
            {
                case RuleOperator.Equal:
                    return Progress.Target > Progress.Actual;
                case RuleOperator.GreaterThan:
                    return Progress.Target <= Progress.Actual;
                case RuleOperator.LessThan:
                    return Progress.Target > Progress.Actual;
                default:
                    throw new ArgumentException(Rule.Operator + " not known!");
            }
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        /// <returns></returns>
        public override string GetAchievementMessage()
        {
            CalculateProgressStatus(false);

            if (Progress.Success.HasValue && Progress.Success.Value)
            {
                if (Rule.Goal == RuleGoal.NumberOfSwitchesTo)
                {
                    return "Congratulations, you reached your goal! You switched " + Progress.Actual + " times to this activity.";
                }
                else if (Rule.Goal == RuleGoal.TimeSpentOn)
                {
                    return "Congratulations, you reached your goal! You spent " + Progress.Actual + " hours on this activity.";
                }
            }
            else
            {
                if (Rule.Goal == RuleGoal.NumberOfSwitchesTo)
                {
                    switch (Rule.Operator)
                    {
                        case RuleOperator.Equal:
                            return "Unfortunately, you missed your goal this time. You switched " + Progress.Actual + " times while you wanted to switch " + Progress.Target + " times.";

                        case RuleOperator.GreaterThan:
                            return "Unfortunately, you missed your goal this time. You switched " + (Progress.Target - Progress.Actual) + " (+" + ((Progress.Target - Progress.Actual) / Progress.Target * 100).ToString("N0") + "%) less than your goal.";

                        case RuleOperator.LessThan:
                            return "Unfortunately, you missed your goal this time. You switched " + (Progress.Actual - Progress.Target) + " (+" + (Progress.Actual / Progress.Target * 100).ToString("N0") + "%) more than your goal.";
                    }
                }
                else if (Rule.Goal == RuleGoal.TimeSpentOn)
                {
                    switch (Rule.Operator)
                    {
                        case RuleOperator.Equal:
                            return "Unfortunately, you missed your goal this time. You spent " + Progress.Actual.ToString("N2") + " hours on this activity while you wanted to spend " + Progress.Target.ToString("N2") + " hours.";

                        case RuleOperator.GreaterThan:
                            return "Unfortunately, you missed your goal this time. You spent " + (Progress.Target - Progress.Actual).ToString("N2") + " (+" + ((Progress.Target - Progress.Actual) / Progress.Target * 100).ToString("N0") + "%) hours less than your goal on this activity.";

                        case RuleOperator.LessThan:
                            return "Unfortunately, you missed your goal this time. You spent " + (Progress.Actual - Progress.Target).ToString("N2") + " (+" + (Progress.Actual / Progress.Target * 100).ToString("N0") + "%) hours more than your goal on this activity.";
                    }
                }
            }
            return "Unknown progress towards this goal";
        }
    }
}