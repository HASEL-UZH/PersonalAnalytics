// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-13
// 
// Licensed under the MIT License.

using System.Windows.Controls;
using GoalSetting.Model;
using Shared.Helpers;
using GoalSetting.Rules;
using Shared.Data;
using System.Windows;
using System.ComponentModel;
using System;

namespace GoalSetting.Views
{
    /// <summary>
    /// Interaction logic for TimeSpent.xaml
    /// </summary>
    public partial class TimeSpent : UserControl
    {
        private AddRule _parent;
        
        public TimeSpent(GoalDomain goalDomain, AddRule parent)
        {
            InitializeComponent();
            
            this._parent = parent;

            Operator.ItemsSource = FormatStringHelper.GetDescriptions(typeof(Operator));
            Timespan.ItemsSource = FormatStringHelper.GetDescriptions(typeof(RuleTimeSpan));
            Activity.ItemsSource = FormatStringHelper.GetDescriptions(typeof(ContextCategory));
            TimeUnit.ItemsSource = FormatStringHelper.GetDescriptions(typeof(TimeUnit));

            switch (goalDomain)
            {
                case GoalDomain.Browsing:
                    Activity.SelectedItem = FormatStringHelper.GetDescription(ContextCategory.WorkUnrelatedBrowsing);
                    break;
                case GoalDomain.Coding:
                    Activity.SelectedItem = FormatStringHelper.GetDescription(ContextCategory.DevCode);
                    break;
                case GoalDomain.Emails:
                    Activity.SelectedItem = FormatStringHelper.GetDescription(ContextCategory.Email);
                    break;
                case GoalDomain.Meetings:
                    Activity.SelectedItem = FormatStringHelper.GetDescription(ContextCategory.PlannedMeeting);
                    break;
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string title = Title.Text;
            Operator op = FormatStringHelper.GetValueFromDescription<Operator>(Operator.SelectedItem.ToString());

            double targetValue = slValue.Value;

            TimeUnit selectedTimeUnit = FormatStringHelper.GetValueFromDescription<TimeUnit>(TimeUnit.SelectedItem.ToString());
            switch (selectedTimeUnit)
            {
                case Views.TimeUnit.Minutes:
                    targetValue = TimeSpan.FromMinutes(targetValue).TotalMilliseconds;
                    break;
                case Views.TimeUnit.Hours:
                    targetValue = TimeSpan.FromHours(targetValue).TotalMilliseconds;
                    break;
                case Views.TimeUnit.Days:
                    targetValue = TimeSpan.FromDays(targetValue).TotalMilliseconds;
                    break;
            }


            Rule rule = new Rule { Goal = Goal.TimeSpentOn, Operator = op, TargetValue = targetValue.ToString() };
            ContextCategory activity = FormatStringHelper.GetValueFromDescription<ContextCategory>(Activity.SelectedItem.ToString());
            RuleTimeSpan timespan = FormatStringHelper.GetValueFromDescription<RuleTimeSpan>(Timespan.SelectedItem.ToString());

            PARule newRule = new PARule { Title = title, Rule = rule, Activity = activity, TimeSpan = timespan, TimePoint = null, IsVisualizationEnabled = true };
            GoalSettingManager.Instance.AddRule(newRule);
            this.Visibility = Visibility.Collapsed;
            _parent.Step_4();
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
            if (Operator.SelectedIndex != -1 && Timespan.SelectedIndex != -1 && Activity.SelectedIndex != -1 && TimeUnit.SelectedIndex != -1 && !string.IsNullOrEmpty(Title.Text))
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