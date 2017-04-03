// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-13
// 
// Licensed under the MIT License.

using GoalSetting.Model;
using GoalSetting.Rules;
using Shared.Data;
using Shared.Helpers;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace GoalSetting.Views
{
    /// <summary>
    /// Interaction logic for EmailInbox.xaml
    /// </summary>
    public partial class EmailInbox : UserControl
    {
        private AddRule _parent;
        private bool _isRuleEditing = false;
        private PARule _oldRule;

        public EmailInbox(AddRule parent) : this()
        {
            this._parent = parent;
        }

        public EmailInbox(PARule rule) : this()
        {
            this._oldRule = rule;
            this._isRuleEditing = true;
            Title.Text = rule.Title;
            Checkpoint.SelectedItem = FormatStringHelper.GetDescription(rule.TimePoint);
            EnterTime.Text = rule.Time;
            Operator.SelectedItem = FormatStringHelper.GetDescription(rule.Rule.Operator);
            slValue.Value = double.Parse(rule.Rule.TargetValue);
        }

        private EmailInbox()
        {
            InitializeComponent();
            Operator.ItemsSource = FormatStringHelper.GetDescriptions(typeof(Operator));
            Checkpoint.ItemsSource = FormatStringHelper.GetDescriptions(typeof(RuleTimePoint));
        }

        private void Title_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValuesUpdated();
        }

        private void Values_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (FormatStringHelper.GetValueFromDescription<RuleTimePoint>(Checkpoint.SelectedItem.ToString()) == RuleTimePoint.Timepoint)
            {
                TimeHint.Visibility = Visibility.Visible;
                EnterTime.Visibility = Visibility.Visible;
            }
            else
            {
                TimeHint.Visibility = Visibility.Collapsed;
                EnterTime.Visibility = Visibility.Collapsed;
            }

            ValuesUpdated();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string title = Title.Text;
            Operator op = FormatStringHelper.GetValueFromDescription<Operator>(Operator.SelectedItem.ToString());

            double targetValue = slValue.Value;

            Rule rule = new Rule { Goal = Goal.NumberOfEmailsInInbox, Operator = op, TargetValue = targetValue.ToString() };
            ContextCategory activity = ContextCategory.Email;
            RuleTimePoint timepoint = FormatStringHelper.GetValueFromDescription<RuleTimePoint>(Checkpoint.SelectedItem.ToString());

            string time = string.Empty;
            if (timepoint == RuleTimePoint.Timepoint)
            {
                time = EnterTime.Text;
            }

            PARule newRule = new PARule { Title = title, Rule = rule, Activity = activity, TimePoint = timepoint, Time = time, TimeSpan = null, IsVisualizationEnabled = true };
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

        private void ValuesUpdated()
        {
            if (Operator.SelectedIndex != -1 && Checkpoint.SelectedIndex != -1 && !string.IsNullOrEmpty(Title.Text))
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