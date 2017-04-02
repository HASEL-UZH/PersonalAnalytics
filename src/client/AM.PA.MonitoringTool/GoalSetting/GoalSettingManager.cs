// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

using GoalSetting.Data;
using GoalSetting.Model;
using GoalSetting.Rules;
using GoalSetting.Views;
using Shared;
using Shared.Data;
using Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Shared.Events;

namespace GoalSetting
{
    public class GoalSettingManager
    {
        private ObservableCollection<PARule> _rules;

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

        internal List<PARule> GetRules()
        {
            return _rules.ToList();
        }

        internal void AddNewRule()
        {
            Window window = new Window
            {
                Title = "Goal Setting Dashboard",
                Content = new AddRule(_rules),
                SizeToContent = SizeToContent.WidthAndHeight
            };
            window.ShowDialog();
        }

        internal void AddRule(PARule newRule)
        {
            _rules.Add(newRule);
        }

        /// <summary>
        /// Starts the goal setting manager. This method is called whenever the user clicks on 'Goal setting' in the context menu.
        /// </summary>
        public void Start()
        {
            DatabaseConnector.CreateRulesTableIfNotExists();

            _rules = DatabaseConnector.GetStoredRules();
            
            Window window = new Window
            {
                Title = "Goal Setting Dashboard",
                Content = new GoalSetting(_rules),
                SizeToContent = SizeToContent.WidthAndHeight
            };
            window.ShowDialog();
            
        }

        public void OnNewTrackerEvent(object sender, TrackerEvents e)
        {
            if (e is ActivitySwitchEvent)
            {
                var dto = new ContextDto { Context = new ContextInfos { ProgramInUse = (e as ActivitySwitchEvent).NewProcessName, WindowTitle = (e as ActivitySwitchEvent).NewWindowTitle } };
                ContextCategory activity = ContextMapper.GetContextCategory(dto);
                Console.WriteLine("New activity: " + activity);
            }
        }

        internal void DeleteRule(PARule rule)
        {
            Logger.WriteToConsole("Delete: " + rule);
            _rules.Remove(rule);
        }

        private List<Activity> GetActivity(RuleTimeSpan timespan)
        {
            List<ActivityContext> activities = new List<ActivityContext>();

            switch (timespan)
            {
                case RuleTimeSpan.EveryDay:
                    activities = DatabaseConnector.GetActivitiesSince(new DateTime(DateTimeHelper.GetStartOfDay(DateTime.Now).Ticks));
                    break;

                case RuleTimeSpan.Week:
                    activities = DatabaseConnector.GetActivitiesSince(new DateTime(DateTimeHelper.GetFirstDayOfWeek_Iso8801(DateTime.Now).Ticks));
                    break;

                case RuleTimeSpan.Month:
                    activities = DatabaseConnector.GetActivitiesSince(new DateTime(DateTimeHelper.GetStartOfMonth(DateTime.Now).Ticks));
                    break;

                case RuleTimeSpan.Afternoon:
                    activities = DatabaseConnector.GetActivitiesSince(new DateTime(DateTimeHelper.GetNoonOfDay(DateTime.Now).Ticks));
                    break;

                case RuleTimeSpan.Morning:
                    activities = DatabaseConnector.GetActivitiesSinceAndBefore(new DateTime(DateTimeHelper.GetStartOfDay(DateTime.Now).Ticks), new DateTime(DateTimeHelper.GetNoonOfDay(DateTime.Now).Ticks));
                    break;

                case RuleTimeSpan.Friday:
                    DateTimeOffset friday = DateTimeHelper.GetPreviousSpecificDay(DateTime.Now, DayOfWeek.Friday);
                    activities = DatabaseConnector.GetActivitiesSinceAndBefore(new DateTime(friday.Ticks), new DateTime(DateTimeHelper.GetEndOfDay(friday).Ticks));
                    break;

                case RuleTimeSpan.Monday:
                    DateTimeOffset monday = DateTimeHelper.GetPreviousSpecificDay(DateTime.Now, DayOfWeek.Monday);
                    activities = DatabaseConnector.GetActivitiesSinceAndBefore(new DateTime(monday.Ticks), new DateTime(DateTimeHelper.GetEndOfDay(monday).Ticks));
                    break;

                case RuleTimeSpan.Saturday:
                    DateTimeOffset saturday = DateTimeHelper.GetPreviousSpecificDay(DateTime.Now, DayOfWeek.Saturday);
                    activities = DatabaseConnector.GetActivitiesSinceAndBefore(new DateTime(saturday.Ticks), new DateTime(DateTimeHelper.GetEndOfDay(saturday).Ticks));
                    break;

                case RuleTimeSpan.Sunday:
                    DateTimeOffset sunday = DateTimeHelper.GetPreviousSpecificDay(DateTime.Now, DayOfWeek.Sunday);
                    activities = DatabaseConnector.GetActivitiesSinceAndBefore(new DateTime(sunday.Ticks), new DateTime(DateTimeHelper.GetEndOfDay(sunday).Ticks));
                    break;

                case RuleTimeSpan.Thursday:
                    DateTimeOffset thursday = DateTimeHelper.GetPreviousSpecificDay(DateTime.Now, DayOfWeek.Thursday);
                    activities = DatabaseConnector.GetActivitiesSinceAndBefore(new DateTime(thursday.Ticks), new DateTime(DateTimeHelper.GetEndOfDay(thursday).Ticks));
                    break;

                case RuleTimeSpan.Tuesday:
                    DateTimeOffset tuesday = DateTimeHelper.GetPreviousSpecificDay(DateTime.Now, DayOfWeek.Tuesday);
                    activities = DatabaseConnector.GetActivitiesSinceAndBefore(new DateTime(tuesday.Ticks), new DateTime(DateTimeHelper.GetEndOfDay(tuesday).Ticks));
                    break;

                case RuleTimeSpan.Wednesday:
                    DateTimeOffset wednesday = DateTimeHelper.GetPreviousSpecificDay(DateTime.Now, DayOfWeek.Wednesday);
                    activities = DatabaseConnector.GetActivitiesSinceAndBefore(new DateTime(wednesday.Ticks), new DateTime(DateTimeHelper.GetEndOfDay(wednesday).Ticks));
                    break;
            }

            activities = DataHelper.MergeSameActivities(activities, Settings.MinimumSwitchTime);

            List<Activity> result = new List<Activity>();

            foreach (ContextCategory category in Enum.GetValues(typeof(ContextCategory)))
            {
                Activity activity = new Activity
                {
                    Category = category.ToString(),
                    TimeSpentOn = DataHelper.GetTotalTimeSpentOnActivity(activities, category).TotalMilliseconds,
                    NumberOfSwitchesTo = DataHelper.GetNumberOfSwitchesToActivity(activities, category),
                    Context = activities.Where(a => a.Activity.Equals(category)).ToList()
                };
                result.Add(activity);
            }
            return result;
        }

        public delegate void OnOpenRetrospectionFromGoalSetting(VisType type);
        public event OnOpenRetrospectionFromGoalSetting OpenRetrospectionEvent;

        internal void OpenRetrospection(VisType type)
        {
            OpenRetrospectionEvent?.Invoke(type);
        }

        internal void DeleteCachedResults()
        {
            activitiesMap.Clear();
        }

        Dictionary<RuleTimeSpan, List<Activity>> activitiesMap = new Dictionary<RuleTimeSpan, List<Activity>>();

        public void CheckRules(ObservableCollection<PARule> rules)
        {
            
            foreach (PARule rule in rules)
            {
                //We can only do that for rules that have a timespan
                if (rule.TimeSpan.HasValue)
                {
                    //if we do not yet have the activities, we have to get them!
                    if (!activitiesMap.ContainsKey(rule.TimeSpan.Value))
                    {
                        activitiesMap.Add(rule.TimeSpan.Value, GetActivity(rule.TimeSpan.Value));
                    }

                    List<Activity> activities = null;
                    activitiesMap.TryGetValue(rule.TimeSpan.Value, out activities);

                    if (activities != null)
                    {
                        foreach (Activity activity in activities)
                        {
                            if (activity.Category.Equals(rule.Activity.ToString()))
                            {
                                Logger.WriteToConsole("" + activity);
                                Logger.WriteToConsole("" + rule);
                                rule.Compile();

                                //Store results in PARule
                                rule.Progress.Success = rule.CompiledRule(activity);
                                rule.Progress.Time = activity.GetTimeSpentInHours();
                                rule.Progress.Switches = activity.NumberOfSwitchesTo;
                                rule.CalculateProgressStatus();
                            }
                        }
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                var popup = new RulePopUp(rules);
                popup.ShowDialog();
            }
            ));

        }

    }
}