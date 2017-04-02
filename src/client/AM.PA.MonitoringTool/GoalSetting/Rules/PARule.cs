// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

using GoalSetting.Model;
using Shared.Data;
using Shared.Helpers;
using System;

namespace GoalSetting.Rules
{
    public class PARule
    {
        
        public string Title { get; set; }

        public Rule Rule { get; set; }

        public ContextCategory Activity { get; set; }

        public RuleTimeSpan? TimeSpan { get; set; }

        public RuleTimePoint? TimePoint { get; set; }

        public string Time { get; set; }

        public bool IsVisualizationEnabled { get; set; }

        public override string ToString()
        {
            string str = string.Empty;

            switch (Rule.Goal)
            {
                case Goal.NumberOfEmailsInInbox:
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
                    break;

                case Goal.NumberOfSwitchesTo:
                    str += "I want to switch ";
                    str += FormatStringHelper.GetDescription(Rule.Operator).ToLower() + " ";
                    str += Rule.TargetValue + " ";
                    str += "times to ";
                    str += FormatStringHelper.GetDescription(Activity) + " ";
                    str += "per " + FormatStringHelper.GetDescription(TimeSpan) + ".";
                    break;

                case Goal.TimeSpentOn:
                    str += "I want to spend ";
                    str += FormatStringHelper.GetDescription(Rule.Operator).ToLower() + " ";
                    str += Rule.TargetTimeSpan;
                    str += " on " + FormatStringHelper.GetDescription(Activity) + " ";
                    str += "per " + FormatStringHelper.GetDescription(TimeSpan) + ".";
                    break;
            }
            return str;
        }

        private Progress _progress = null;

        public Progress Progress { get { if (_progress == null) { _progress = new Progress(); } return _progress; } set { _progress = value;  } }

        public string Action { get; set; }

        internal Func<Activity, bool> CompiledRule { get; set; }

        public void Compile()
        {
            CompiledRule = RuleEngine.CompileRule<Activity>(Rule);
        }

        internal void CalculateProgressStatus()
        {
            double percentage = Double.NaN;

            switch (Rule.Goal)
            {
                case Goal.TimeSpentOn:
                    double targetTime = Double.Parse(Rule.TargetValue) / 1000 / 60 / 60;
                    double actualTime = string.IsNullOrEmpty(Progress.Time) ? 0.0 : Double.Parse(Progress.Time);
                    percentage = actualTime / targetTime;
                    break;
                case Goal.NumberOfSwitchesTo:
                    double targetSwitches = Double.Parse(Rule.TargetValue);
                    double actualSwitches = Progress.Switches;
                    percentage = actualSwitches / targetSwitches;
                    break;
            }
            
            if (Rule.Operator == Operator.GreaterThan || Rule.Operator == Operator.GreaterThanOrEqual)
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
            else if (Rule.Operator == Operator.LessThan || Rule.Operator == Operator.LessThanOrEqual)
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
        }

        internal string GetProgressMessage()
        {
            return (string.IsNullOrEmpty(Progress.Time) ? "0" : Progress.Time) + " hours / " + Progress.Switches + " switches";
        }
    }
    
    public class Progress
    {
        public ProgressStatus Status { get; set; }
        public bool? Success { get; set; }
        public string Time { get; set; }
        public int Switches { get; set; }
    }

    public enum ProgressStatus
    {
        VeryLow,
        Low,
        Average,
        High,
        VeryHigh
    }

}