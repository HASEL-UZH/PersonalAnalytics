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

        public WorkFragmentation(GoalDomain goalDomain, AddRule parent)
        {
            InitializeComponent();

            _parent = parent;

            Operator.ItemsSource = FormatStringHelper.GetDescriptions(typeof(Operator));
            Timespan.ItemsSource = FormatStringHelper.GetDescriptions(typeof(RuleTimeSpan));
            Activity.ItemsSource = FormatStringHelper.GetDescriptions(typeof(ContextCategory));

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

            PARule newRule = new PARule { Title = title, Rule = rule, Activity = activity, TimeSpan = timespan, TimePoint = null, IsVisualizationEnabled = true };
            GoalSettingManager.Instance.AddRule(newRule);
            this.Visibility = Visibility.Collapsed;
            _parent.Close();
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
