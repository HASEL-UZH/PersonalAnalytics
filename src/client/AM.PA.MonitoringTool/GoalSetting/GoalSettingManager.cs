// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

using GoalSetting.Model;
using GoalSetting.Rules;
using Shared.Data;
using System.Collections.ObjectModel;
using System.Windows;

namespace GoalSetting
{
    public class GoalSettingManager
    {

        private static GoalSettingManager instance;

        private GoalSettingManager() { }

        public static GoalSettingManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GoalSettingManager();
                }
                return instance;
            }
        }

        /// <summary>
        /// Starts the goal setting manager. This method is called whenever the user clicks on 'Goal setting' in the context menu.
        /// </summary>
        public void Start()
        {
            var rules = new ObservableCollection<PARule>();
            
            rules.Add(new PARule() { Rule = new Rule { Goal = Goal.TimeSpentOn.ToString(), Operator = "GreaterThan", TargetValue = "1000" }, Activity = ContextCategory.WorkUnrelatedBrowsing, TimeSpan = RuleTimeSpan.EveryDay });
            rules.Add(new PARule() { Rule = new Rule { Goal = Goal.NumberOfSwitchesTo.ToString(), Operator = "GreaterThan", TargetValue = "2" }, Activity = ContextCategory.Email, TimeSpan = RuleTimeSpan.Week });
            
            Window window = new Window
            {
                Title = "Goal setting dashboard",
                Content = new GoalSetting(rules)
            };
            window.ShowDialog();
        }

    }
}