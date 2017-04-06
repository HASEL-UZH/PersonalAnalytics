// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-13
// 
// Licensed under the MIT License.

using GoalSetting.Goals;
using GoalSetting.Model;
using Shared.Data;
using Shared.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace GoalSetting.Views
{
    /// <summary>
    /// Interaction logic for EmailInbox.xaml
    /// </summary>
    public partial class EmailInbox : UserControl
    {
        private AddGoal _parent;
        private bool _isRuleEditing = false;
        private GoalEmail _oldGoal;

        public EmailInbox(AddGoal parent) : this()
        {
            this._parent = parent;
        }

        public EmailInbox(GoalEmail goal) : this()
        {
            this._oldGoal = goal;
            this._isRuleEditing = true;
            Title.Text = goal.Title;
            Checkpoint.SelectedItem = FormatStringHelper.GetDescription(goal.TimePoint);
            EnterTime.Text = goal.Time;
            Operator.SelectedItem = FormatStringHelper.GetDescription(goal.Rule.Operator);
            slValue.Value = double.Parse(goal.Rule.TargetValue);
        }

        private EmailInbox()
        {
            InitializeComponent();
            Operator.ItemsSource = FormatStringHelper.GetDescriptions(typeof(RuleOperator));
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
            RuleOperator op = FormatStringHelper.GetValueFromDescription<RuleOperator>(Operator.SelectedItem.ToString());

            double targetValue = slValue.Value;

            Rule rule = new Rule { Goal = RuleGoal.NumberOfEmailsInInbox, Operator = op, TargetValue = targetValue.ToString() };
            RuleTimePoint timepoint = FormatStringHelper.GetValueFromDescription<RuleTimePoint>(Checkpoint.SelectedItem.ToString());

            string time = string.Empty;
            if (timepoint == RuleTimePoint.Timepoint)
            {
                time = EnterTime.Text;
            }

            GoalEmail newRule = new GoalEmail { Title = title, Rule = rule, TimePoint = timepoint, Time = time, IsVisualizationEnabled = true };
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