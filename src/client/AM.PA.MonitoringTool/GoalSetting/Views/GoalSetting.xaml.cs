// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Windows.Controls;
using GoalSetting.Model;
using Shared;
using System;
using GoalSetting.Data;
using Shared.Data;
using System.Linq;
using Shared.Helpers;

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

            var activities = DatabaseConnector.GetActivitiesSince(new DateTime(DateTimeHelper.GetStartOfDay(DateTime.Now.AddDays(-10)).Ticks));
            activities = DataHelper.MergeSameActivities(activities, Settings.MinimumSwitchTime);
            
            foreach (ContextCategory category in Enum.GetValues(typeof(ContextCategory))) {

                Activity activity = new Activity
                {
                    Category = category,
                    TotalDuration = DataHelper.GetTotalTimeSpentOnActivity(activities, category),
                    TotalSwitches = DataHelper.GetNumberOfSwitchesToActivity(activities, category),
                    Context = activities.Where(a => a.Activity.Equals(category)).ToList()
                };

                Console.WriteLine(activity);
            }
            
        }
    }

}