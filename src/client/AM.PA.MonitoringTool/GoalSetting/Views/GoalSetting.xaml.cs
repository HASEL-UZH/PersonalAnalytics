// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Windows.Controls;
using GoalSetting.Rules;
using System.Windows;
using System.Collections.Specialized;
using System.ComponentModel;

namespace GoalSetting
{
    /// <summary>
    /// Interaction logic for GoalSetting.xaml
    /// </summary>
    public partial class GoalSetting : UserControl, INotifyPropertyChanged
    {
        private ObservableCollection<PARule> _rules;
        private bool _hasChanged = false;
        
        public bool HasChanged { get { return _hasChanged; } set { _hasChanged = value; NotifyPropertyChanged("HasChanged"); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public GoalSetting(ObservableCollection<PARule> rules)
        {
            InitializeComponent();
            this._rules = rules;
            Rules.SelectionMode = DataGridSelectionMode.Single;
            Rules.ItemsSource = rules;
            _rules.CollectionChanged += _rules_CollectionChanged;
            CheckRules.IsEnabled = _rules.Count > 0;
            SaveRules.DataContext = this;
        }

        private void _rules_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CheckRules.IsEnabled = _rules.Count > 0;
        }

        private void CheckRules_Click(object sender, RoutedEventArgs e)
        {
            GoalSettingManager.Instance.CheckRules(_rules);
        }

        private void SaveRules_Click(object sender, RoutedEventArgs e)
        {
            HasChanged = ! DatabaseConnector.SaveRules(_rules);
        }

        private void AddRule_Click(object sender, RoutedEventArgs e)
        { 
            GoalSettingManager.Instance.AddNewRule();
            HasChanged = true;
        }

        private void DeleteRule_Click(object sender, RoutedEventArgs e)
        {
            PARule rule = (PARule) Rules.SelectedItem;
            GoalSettingManager.Instance.DeleteRule(rule);
            HasChanged = true;
        }

        private void Rules_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            DeleteRule.IsEnabled = Rules.SelectedCells.Count > 0;
        }

    }

}