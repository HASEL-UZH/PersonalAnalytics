// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Windows.Controls;
using GoalSetting.Rules;
using System.Windows;

namespace GoalSetting
{
    /// <summary>
    /// Interaction logic for GoalSetting.xaml
    /// </summary>
    public partial class GoalSetting : UserControl
    {
        private ObservableCollection<PARule> rules;

        public GoalSetting(ObservableCollection<PARule> rules)
        {
            InitializeComponent();
            this.rules = rules;
            this.Rules.ItemsSource = rules;
        }

        private void CheckRules_Click(object sender, RoutedEventArgs e)
        {
            GoalSettingManager.Instance.CheckRules(rules);
        }

        private void SaveRules_Click(object sender, RoutedEventArgs e)
        {
            DatabaseConnector.SaveRules(rules);
        }

        private void AddRule_Click(object sender, RoutedEventArgs e)
        {
            GoalSettingManager.Instance.AddNewRule();
        }

        private void DeleteRule_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Rules_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            DeleteRule.IsEnabled = Rules.SelectedCells.Count > 0;
        }
    }

}