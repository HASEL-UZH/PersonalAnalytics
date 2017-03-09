// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-09
// 
// Licensed under the MIT License.

using System.Windows.Controls;
using GoalSetting.Model;
using Shared.Helpers;
using GoalSetting.Rules;
using Shared.Data;
using System;

namespace GoalSetting.Views
{
    /// <summary>
    /// Interaction logic for WorkFragmentation.xaml
    /// </summary>
    public partial class WorkFragmentation : UserControl
    {
        public WorkFragmentation()
        {
            InitializeComponent();
            Operator.ItemsSource = FormatStringHelper.GetDescriptions(typeof(Operator));
            Timespan.ItemsSource = FormatStringHelper.GetDescriptions(typeof(RuleTimeSpan));
            Activity.ItemsSource = FormatStringHelper.GetDescriptions(typeof(ContextCategory));
        }

        private void Add_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Console.WriteLine("Add rule");
        }

        private void Values_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (Operator.SelectedIndex != -1 && Timespan.SelectedIndex != -1 && Activity.SelectedIndex != -1)
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
