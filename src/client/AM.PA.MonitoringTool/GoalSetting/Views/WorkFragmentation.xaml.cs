// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-09
// 
// Licensed under the MIT License.

using System.Windows.Controls;
using GoalSetting.Model;
using Shared.Helpers;
using GoalSetting.Rules;
using Shared.Data;
using System.Windows;

namespace GoalSetting.Views
{
    /// <summary>
    /// Interaction logic for WorkFragmentation.xaml
    /// </summary>
    public partial class WorkFragmentation : UserControl
    {

        private AddRule _parent;
        private bool _isRuleEditing = false;
        private PARuleActivity _oldRule;

        private WorkFragmentation() {
            InitializeComponent();

            Operator.ItemsSource = FormatStringHelper.GetDescriptions(typeof(Operator));
            Timespan.ItemsSource = FormatStringHelper.GetDescriptions(typeof(RuleTimeSpan));
            Activity.ItemsSource = FormatStringHelper.GetDescriptions(typeof(ContextCategory));
        }

        public WorkFragmentation(PARuleActivity rule) : this()
        {
            this._isRuleEditing = true;
            this._oldRule = rule;

            Title.Text = rule.Title;
            Operator.SelectedItem = FormatStringHelper.GetDescription(rule.Rule.Operator);
            slValue.Value = double.Parse(rule.Rule.TargetValue);
            Timespan.SelectedItem = FormatStringHelper.GetDescription(rule.TimeSpan);
            Activity.SelectedItem = FormatStringHelper.GetDescription(rule.Activity);
        }

        public WorkFragmentation(GoalDomain goalDomain, AddRule parent) : this()
        {
            this._parent = parent;
            
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
            Rule rule = new Rule { Goal = Goal.NumberOfSwitchesTo, Operator = op, TargetValue = slValue.Value.ToString()};
            ContextCategory activity = FormatStringHelper.GetValueFromDescription<ContextCategory>(Activity.SelectedItem.ToString());
            RuleTimeSpan timespan = FormatStringHelper.GetValueFromDescription<RuleTimeSpan>(Timespan.SelectedItem.ToString());

            PARuleActivity newRule = new PARuleActivity { Title = title, Rule = rule, Activity = activity, TimeSpan = timespan, IsVisualizationEnabled = true };
            this.Visibility = Visibility.Collapsed;

            if (!_isRuleEditing)
            {
                GoalSettingManager.Instance.AddRule(newRule);
                _parent.Close();
            }
            else
            {
                GoalSettingManager.Instance.EditRule(_oldRule, newRule);
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
