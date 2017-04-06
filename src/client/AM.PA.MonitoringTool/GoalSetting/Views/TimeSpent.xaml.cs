// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-13
// 
// Licensed under the MIT License.

using System.Windows.Controls;
using GoalSetting.Model;
using Shared.Helpers;
using Shared.Data;
using System.Windows;
using System.ComponentModel;
using System;
using GoalSetting.Goals;

namespace GoalSetting.Views
{
    /// <summary>
    /// Interaction logic for TimeSpent.xaml
    /// </summary>
    public partial class TimeSpent : UserControl
    {
        private AddGoal _parent;
        private GoalActivity _oldGoal;
        private bool _isRuleEditing = false;

        private TimeSpent()
        {
            InitializeComponent();

            Operator.ItemsSource = FormatStringHelper.GetDescriptions(typeof(RuleOperator));
            Timespan.ItemsSource = FormatStringHelper.GetDescriptions(typeof(RuleTimeSpan));
            Activity.ItemsSource = FormatStringHelper.GetDescriptions(typeof(ContextCategory));
            TimeUnitComboBox.ItemsSource = FormatStringHelper.GetDescriptions(typeof(TimeUnit));
        }

        public TimeSpent(GoalActivity goal) : this()
        {
            this._oldGoal = goal;
            this._isRuleEditing = true;

            Title.Text = goal.Title;
            Operator.SelectedItem = FormatStringHelper.GetDescription(goal.Rule.Operator);
            TimeSpan timespan = TimeSpan.FromMilliseconds(double.Parse(goal.Rule.TargetValue));
            
            if (timespan.TotalDays > 1)
            {
                slValue.Value = timespan.TotalDays;
                TimeUnitComboBox.SelectedItem = FormatStringHelper.GetDescription(TimeUnit.Days);
            }
            else if (timespan.TotalHours > 1)
            {
                slValue.Value = timespan.TotalHours;
                TimeUnitComboBox.SelectedItem = FormatStringHelper.GetDescription(TimeUnit.Hours);
            }
            else
            {
                slValue.Value = timespan.TotalMinutes;
                TimeUnitComboBox.SelectedItem = FormatStringHelper.GetDescription(TimeUnit.Minutes);
            }
            
            Activity.SelectedItem = FormatStringHelper.GetDescription(goal.Activity);
            Timespan.SelectedItem = FormatStringHelper.GetDescription(goal.TimeSpan);
        }

        public TimeSpent(RuleGoalDomain goalDomain, AddGoal parent) : this()
        {
            this._parent = parent;

            switch (goalDomain)
            {
                case RuleGoalDomain.Browsing:
                    Activity.SelectedItem = FormatStringHelper.GetDescription(ContextCategory.WorkUnrelatedBrowsing);
                    break;
                case RuleGoalDomain.Coding:
                    Activity.SelectedItem = FormatStringHelper.GetDescription(ContextCategory.DevCode);
                    break;
                case RuleGoalDomain.Emails:
                    Activity.SelectedItem = FormatStringHelper.GetDescription(ContextCategory.Email);
                    break;
                case RuleGoalDomain.Meetings:
                    Activity.SelectedItem = FormatStringHelper.GetDescription(ContextCategory.PlannedMeeting);
                    break;
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string title = Title.Text;
            RuleOperator op = FormatStringHelper.GetValueFromDescription<RuleOperator>(Operator.SelectedItem.ToString());

            double targetValue = slValue.Value;

            TimeUnit selectedTimeUnit = FormatStringHelper.GetValueFromDescription<TimeUnit>(TimeUnitComboBox.SelectedItem.ToString());
            switch (selectedTimeUnit)
            {
                case TimeUnit.Minutes:
                    targetValue = TimeSpan.FromMinutes(targetValue).TotalMilliseconds;
                    break;
                case TimeUnit.Hours:
                    targetValue = TimeSpan.FromHours(targetValue).TotalMilliseconds;
                    break;
                case TimeUnit.Days:
                    targetValue = TimeSpan.FromDays(targetValue).TotalMilliseconds;
                    break;
            }
            
            Rule rule = new Rule { Goal = RuleGoal.TimeSpentOn, Operator = op, TargetValue = targetValue.ToString() };
            ContextCategory activity = FormatStringHelper.GetValueFromDescription<ContextCategory>(Activity.SelectedItem.ToString());
            RuleTimeSpan timespan = FormatStringHelper.GetValueFromDescription<RuleTimeSpan>(Timespan.SelectedItem.ToString());

            GoalActivity newRule = new GoalActivity { Title = title, Rule = rule, Activity = activity, TimeSpan = timespan, IsVisualizationEnabled = true };
            this.Visibility = Visibility.Collapsed;

            if (!_isRuleEditing)
            {
                GoalSettingManager.Instance.AddGoal(newRule);
                _parent.Close();
            }
            else
            {
                GoalSettingManager.Instance.EditGoal(_oldGoal, newRule);
                (this.Parent as Window).Close();
            }
        }

        private void Values_Changed(object sender, SelectionChangedEventArgs e)
        {
            ValuesUpdated();
        }

        private void Title_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValuesUpdated();
        }

        private void ValuesUpdated()
        {
            if (Operator.SelectedIndex != -1 && Timespan.SelectedIndex != -1 && Activity.SelectedIndex != -1 && TimeUnitComboBox.SelectedIndex != -1 && !string.IsNullOrEmpty(Title.Text))
            {
                Add.IsEnabled = true;
            }
            else
            {
                Add.IsEnabled = false;
            }
        }
    }

    public enum TimeUnit
    {
        [Description("minutes")]
        Minutes,

        [Description("hours")]
        Hours,

        [Description("days")]
        Days,
    }
}