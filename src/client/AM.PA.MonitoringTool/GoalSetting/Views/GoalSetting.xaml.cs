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
using GoalSetting.Rules;
using System.Collections.Generic;

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

        private void CheckRules_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            List<Func<Activity,bool>> compiledRules = new List<Func<Activity, bool>>();

            Logger.WriteToConsole("Check for rules: ");
            foreach (PARule rule in rules)
            {
                Logger.WriteToConsole(rule.ToString());
                compiledRules.Add(RuleEngine.CompileRule<Activity>(rule.Rule));
            }
    
            var activities = DatabaseConnector.GetActivitiesSince(new DateTime(DateTimeHelper.GetStartOfDay(DateTime.Now.AddDays(-10)).Ticks));
            activities = DataHelper.MergeSameActivities(activities, Settings.MinimumSwitchTime);

            foreach (ContextCategory category in Enum.GetValues(typeof(ContextCategory))) {

                Activity activity = new Activity
                {
                    Category = category.ToString(),
                    TimeSpentOn = DataHelper.GetTotalTimeSpentOnActivity(activities, category).TotalMilliseconds,
                    NumberOfSwitchesTo = DataHelper.GetNumberOfSwitchesToActivity(activities, category),
                    Context = activities.Where(a => a.Activity.Equals(category)).ToList()
                };

                Console.WriteLine(activity);
                foreach (Func<Activity,bool> cRule in compiledRules)
                {
                    Console.WriteLine(cRule.ToString());
                    Console.WriteLine(cRule(activity));
                }
            }
            
        }
    }

}