// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

using GoalSetting.Data;
using GoalSetting.Model;
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
using System.Diagnostics;
using GoalSetting.Goals;
using System.Timers;

namespace GoalSetting
{
    public class GoalSettingManager
    {
        private ObservableCollection<Goal> _goals;

        private static GoalSettingManager instance;

        private Timer _goalCheckerTimer;

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

        internal List<Goal> GetGoals()
        {
            return _goals.ToList();
        }

        internal void AddNewGoal()
        {
            Window window = new Window
            {
                Title = "Goal Setting Dashboard",
                Content = new AddGoal(_goals),
                SizeToContent = SizeToContent.WidthAndHeight
            };
            window.ShowDialog();
        }

        internal void AddGoal(Goal newGoal)
        {
            _goals.Add(newGoal);
            DatabaseConnector.AddGoal(newGoal);
        }

        internal List<GoalActivity> GetActivityGoals()
        {
            return _goals.OfType<GoalActivity>().ToList();
        }

        /// <summary>
        /// Starts the goal setting manager. This method is called whenever the user clicks on 'Goal setting' in the context menu.
        /// </summary>
        public void Start()
        {
            DatabaseConnector.CreateGoalsTableIfNotExists();

            _goals = DatabaseConnector.GetStoredGoals();

            StartGoalCheckingTimer();
        }

        private void StartGoalCheckingTimer()
        {
            _goalCheckerTimer = new Timer();
            _goalCheckerTimer.Elapsed += _goalCheckerTimer_Elapsed;
            int currentMinute = DateTime.Now.Minute;
            _goalCheckerTimer.Interval = currentMinute > 5 ? TimeSpan.FromMinutes(65 - currentMinute).TotalMilliseconds : TimeSpan.FromMinutes(5 - currentMinute).TotalMilliseconds;
            Console.WriteLine("Set interval to: " + _goalCheckerTimer.Interval);
            _goalCheckerTimer.Enabled = true;
        }

        private void _goalCheckerTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (var goal in GoalSettingManager.instance.GetGoals())
            {
                goal.CalculateProgressStatus();
            }
            _goalCheckerTimer.Interval = TimeSpan.FromHours(1).TotalMilliseconds;
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

        internal void DeleteGoal(Goal goal)
        {
            Logger.WriteToConsole("Delete: " + goal);
            _goals.Remove(goal);
            DatabaseConnector.RemoveGoal(goal);
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

        internal void EditGoal(Goal oldGoal, Goal newGoal)
        {
            DeleteGoal(oldGoal);
            AddGoal(newGoal);
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
        
        public void CheckRules(ObservableCollection<Goal> goals, bool showPopup)
        {

            foreach (Goal goal in goals)
            {
                //time spent on or switches to activities
                if (goal.Rule.Goal == RuleGoal.NumberOfSwitchesTo || goal.Rule.Goal == RuleGoal.TimeSpentOn)
                {

                    //We can only do that for rules that have a timespan
                    if ((goal as GoalActivity).TimeSpan.HasValue)
                    {
                        //if we do not yet have the activities, we have to get them!
                        if (!activitiesMap.ContainsKey((goal as GoalActivity).TimeSpan.Value))
                        {
                            activitiesMap.Add((goal as GoalActivity).TimeSpan.Value, GetActivity((goal as GoalActivity).TimeSpan.Value));
                        }

                        List<Activity> activities = null;
                        activitiesMap.TryGetValue((goal as GoalActivity).TimeSpan.Value, out activities);

                        if (activities != null)
                        {
                            foreach (Activity activity in activities)
                            {
                                if (activity.Category.Equals((goal as GoalActivity).Activity.ToString()))
                                {
                                    Logger.WriteToConsole("" + activity);
                                    Logger.WriteToConsole("" + goal);
                                    goal.Compile();

                                    //Store results in PARule
                                    goal.Progress.Success = goal.CompiledRule(activity);
                                    goal.Progress.Time = activity.GetTimeSpentInHours();
                                    goal.Progress.Switches = activity.NumberOfSwitchesTo;
                                    goal.CalculateProgressStatus();
                                }
                            }
                        }
                    }
                }
                //Emails
                else if (goal.Rule.Goal == RuleGoal.NumberOfEmailsInInbox)
                {
                    var inbox = DatabaseConnector.GetLatestEmailInboxCount();
                    Trace.WriteLine("Inbox: " + inbox);
                }
            }

            if (showPopup)
            {

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                {
                    var popup = new GoalsPopUp(goals);
                    popup.ShowDialog();
                }));
            }

        }

        public void OpenMainWindow()
        {
            Window window = new Window
            {
                Title = "Goal Setting Dashboard",
                Content = new GoalSetting(),
                SizeToContent = SizeToContent.WidthAndHeight
            };
            window.ShowDialog();
        }
    }
}