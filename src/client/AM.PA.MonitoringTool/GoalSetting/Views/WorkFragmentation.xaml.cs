// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-09
// 
// Licensed under the MIT License.

using System.Windows.Controls;
using GoalSetting.Model;
using Shared.Helpers;
using Shared.Data;
using System.Windows;
using GoalSetting.Goals;

namespace GoalSetting.Views
{
    /// <summary>
    /// Interaction logic for WorkFragmentation.xaml
    /// </summary>
    public partial class WorkFragmentation : UserControl
    {

        private AddGoal _parent;
        private bool _isRuleEditing = false;
        private GoalActivity _oldGoal;

        private WorkFragmentation() {
            InitializeComponent();

            Operator.ItemsSource = FormatStringHelper.GetDescriptions(typeof(RuleOperator));
            Timespan.ItemsSource = FormatStringHelper.GetDescriptions(typeof(RuleTimeSpan));
            Activity.ItemsSource = FormatStringHelper.GetDescriptions(typeof(ContextCategory));
        }

        public WorkFragmentation(GoalActivity goal) : this()
        {
            this._isRuleEditing = true;
            this._oldGoal = goal;

            Title.Text = goal.Title;
            Operator.SelectedItem = FormatStringHelper.GetDescription(goal.Rule.Operator);
            slValue.Value = double.Parse(goal.Rule.TargetValue);
            Timespan.SelectedItem = FormatStringHelper.GetDescription(goal.TimeSpan);
            Activity.SelectedItem = FormatStringHelper.GetDescription(goal.Activity);
        }

        public WorkFragmentation(RuleGoalDomain goalDomain, AddGoal parent) : this()
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
            Rule rule = new Rule { Goal = RuleGoal.NumberOfSwitchesTo, Operator = op, TargetValue = slValue.Value.ToString()};
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
            if (Operator.SelectedIndex != -1 && Timespan.SelectedIndex != -1 && Activity.SelectedIndex != -1 && !string.IsNullOrEmpty(Title.Text))
            {
                Add.IsEnabled = true;
            }
            else
            {
                Add.IsEnabled = false;
            }
        }
    }
}
