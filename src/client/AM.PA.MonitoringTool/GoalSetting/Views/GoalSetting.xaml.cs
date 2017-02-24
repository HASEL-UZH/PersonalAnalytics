// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Windows.Controls;
using GoalSetting.Model;
using Shared;

namespace GoalSetting
{
    /// <summary>
    /// Interaction logic for GoalSetting.xaml
    /// </summary>
    public partial class GoalSetting : UserControl
    {
        private ObservableCollection<Rule> rules;

        public GoalSetting(ObservableCollection<Rule> rules)
        {
            InitializeComponent();
            this.rules = rules;
            this.Rules.ItemsSource = rules;
        }

        private void CheckRules_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Logger.WriteToConsole("Check for rules: ");
            foreach (Rule rule in rules)
            {
                Logger.WriteToConsole(rule.ToString());
            }
        }
    }

}