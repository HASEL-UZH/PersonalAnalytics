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
        private ObservableCollection<PARule> _rules;

        public GoalSetting(ObservableCollection<PARule> rules)
        {
            InitializeComponent();
            this._rules = rules;
            Rules.ItemsSource = rules;
            _rules.CollectionChanged += _rules_CollectionChanged;
            CheckRules.IsEnabled = _rules.Count > 0;
        }

        private void _rules_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            CheckRules.IsEnabled = _rules.Count > 0;
        }

        private void CheckRules_Click(object sender, RoutedEventArgs e)
        {
            GoalSettingManager.Instance.CheckRules(_rules);
        }

        private void SaveRules_Click(object sender, RoutedEventArgs e)
        {
            DatabaseConnector.SaveRules(_rules);
        }

        private void AddRule_Click(object sender, RoutedEventArgs e)
        {
            GoalSettingManager.Instance.AddNewRule();
        }

        private void DeleteRule_Click(object sender, RoutedEventArgs e)
        {
            //TODO
        }

        private void Rules_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            DeleteRule.IsEnabled = Rules.SelectedCells.Count > 0;
        }

    }

}