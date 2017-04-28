// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-27
// 
// Licensed under the MIT License.

using System;
using Shared;
using GoalSetting.Goals;
using GoalSetting.Model;

namespace GoalSetting.Visualizers
{
    public abstract class PAVisualization : BaseVisualization, IVisualization
    {
        protected DateTimeOffset _date;
        protected GoalActivity _goal;

        public PAVisualization(DateTimeOffset date)
        {
            this._date = date;
            IsEnabled = true;
            Size = VisSize.Wide;
            Order = 0;
            CalculateColorCodes();
        }

        public PAVisualization(DateTimeOffset date, GoalActivity goal)
        {
            this._goal = goal;
            this._date = date;
            Title = goal.Title + ": " + goal.ToString();
            IsEnabled = true;
            Size = VisSize.Wide;
            Order = 0;
            goal.CalculateProgressStatus(false);
            CalculateColorCodes();
        }

        public abstract override string GetHtml();

        internal string Color1 { get; set; }
        internal string Color2 { get; set; }
        internal string Pattern1 { get; set; }
        internal string Pattern2 { get; set; }

        private void CalculateColorCodes()
        {
            switch (_goal.Rule.Operator)
            {
                case RuleOperator.Equal:
                    Color1 = GoalVisHelper.GetVeryLowColor();
                    Color2 = GoalVisHelper.GetVeryLowColor();
                    Pattern1 = "#error-pattern";
                    Pattern2 = "#error-pattern";
                    break;
                case RuleOperator.LessThan:
                    Color1 = GoalVisHelper.GetVeryHighColor();
                    Color2 = GoalVisHelper.GetVeryLowColor();
                    Pattern1 = "#success-pattern";
                    Pattern2 = "#error-pattern";
                    break;
                case RuleOperator.GreaterThan:
                    Color1 = GoalVisHelper.GetVeryLowColor();
                    Color2 = GoalVisHelper.GetVeryHighColor();
                    Pattern1 = "#error-pattern";
                    Pattern2 = "#success-pattern";
                    break;
            }
        }
    }

}