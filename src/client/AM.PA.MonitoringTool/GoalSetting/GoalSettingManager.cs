// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

using GoalSetting.Views;
using Shared;
using Shared.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Shared.Events;
using GoalSetting.Goals;
using System.Timers;
using GoalSetting.Model;
using Shared.Helpers;
using GoalSetting.Data;

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

        #region Goal List

        /// <summary>
        /// Returns all goals that are known
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<Goal> GetGoals()
        {
            return _goals;
        }
        
        /// <summary>
        /// Returns all GoalActivity goals
        /// </summary>
        /// <returns></returns>
        internal List<GoalActivity> GetActivityGoals()
        {
            return _goals.OfType<GoalActivity>().ToList();
        }

        /// <summary>
        /// REturns all GoalEmail goals
        /// </summary>
        /// <returns></returns>
        internal List<GoalEmail> GetEmailGoals()
        {
            return _goals.OfType<GoalEmail>().ToList();
        }

        #endregion Goal List

        /// <summary>
        /// Starts the goal setting manager. This method is called whenever the user clicks on 'Goal setting' in the context menu.
        /// </summary>
        public void Start()
        {
            DatabaseConnector.CreateGoalsTableIfNotExists();

            _goals = DatabaseConnector.GetStoredGoals();

            StartGoalCheckingTimer();
        }

        #region Goal Checking Timer

        /// <summary>
        /// Starts a timer that should be triggered every 30 minutes. First at xx:32 or xx:02, whatever is closer.
        /// </summary>
        private void StartGoalCheckingTimer()
        {
            _goalCheckerTimer = new Timer();
            _goalCheckerTimer.Elapsed += _goalCheckerTimer_Elapsed;
            int currentMinute = DateTime.Now.Minute;

            //xx:02
            double interval1 = currentMinute > 2 ? TimeSpan.FromMinutes(62 - currentMinute).TotalMilliseconds : TimeSpan.FromMinutes(2 - currentMinute).TotalMilliseconds + 1;
            //xx:32
            double interval2 = currentMinute > 32 ? TimeSpan.FromMinutes(32 - currentMinute).TotalMilliseconds : TimeSpan.FromMinutes(32 - currentMinute).TotalMilliseconds + 1;

            _goalCheckerTimer.Interval = interval1 < 0 ? interval2 : interval1;
            _goalCheckerTimer.Enabled = true;
        }

        /// <summary>
        /// Called when the timer that checks goal every 30 minutes is trigger. This method then calculates the progress of each goal and starts a new timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _goalCheckerTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (var goal in GoalSettingManager.Instance.GetGoals())
            {
                goal.CalculateProgressStatus(true);
            }
            _goalCheckerTimer.Interval = TimeSpan.FromMinutes(30).TotalMilliseconds;
        }
        
        #endregion Goal Checking Timer

        #region Events
        /// <summary>
        /// Called when retrospection forwards new events from other trackers. This method then checks whether it is an ActivitSwitchEvent.
        /// TODO: we have to define what happens then an define actions!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnNewTrackerEvent(object sender, TrackerEvents e)
        {
            if (e is ActivitySwitchEvent)
            {
                var dto = new ContextDto { Context = new ContextInfos { ProgramInUse = (e as ActivitySwitchEvent).NewProcessName, WindowTitle = (e as ActivitySwitchEvent).NewWindowTitle } };
                ContextCategory activity = ContextMapper.GetContextCategory(dto);
                Console.WriteLine("New activity: " + activity);
            }
        }
        #endregion

        #region Manipulate Goals

        /// <summary>
        /// Deletes a goal. Actually in this case, the goal is marked as inactive in the database.
        /// We do it like that to keep track of all the goals a user has every defined.
        /// </summary>
        /// <param name="goal"></param>
        internal void DeleteGoal(Goal goal)
        {
            Logger.WriteToConsole("Delete: " + goal);
            _goals.Remove(goal);
            DatabaseConnector.RemoveGoal(goal);
        }

        /// <summary>
        /// Edits a goal. In this case, the original goal is marked as inactive in the database and a new goal is added.
        /// We do it like that to keep track of all the goals a user has every defined.
        /// </summary>
        /// <param name="oldGoal"></param>
        /// <param name="newGoal"></param>
        internal void EditGoal(Goal oldGoal, Goal newGoal)
        {
            DeleteGoal(oldGoal);
            AddGoal(newGoal);
        }
        
        /// <summary>
        /// Adds a new goal to the database
        /// </summary>
        /// <param name="newGoal"></param>
        internal void AddGoal(Goal newGoal)
        {
            _goals.Add(newGoal);
            DatabaseConnector.AddGoal(newGoal);
        }

        #endregion Manipulate Goals

        #region Retrospection
        public delegate void OnOpenRetrospectionFromGoalSetting(VisType type);
        public event OnOpenRetrospectionFromGoalSetting OpenRetrospectionEvent;

        /// <summary>
        /// Opens the retrospection from the goal setting main UI
        /// </summary>
        /// <param name="type"></param>
        internal void OpenRetrospection(VisType type)
        {
            OpenRetrospectionEvent?.Invoke(type);
        }
        #endregion Retrospection

        #region UI
        /// <summary>
        /// Opens the Main Window if the goal setting is nôt started within the retrospection
        /// </summary>
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

        /// <summary>
        /// Starts a UI that allows users to add a new goal
        /// </summary>
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
        #endregion

        #region Activities
        /// <summary>
        /// Returns all activities for a specific timespan. Also some postprocessing is done, such as merging activities.
        /// </summary>
        /// <param name="timespan"></param>
        /// <returns></returns>
        public List<Activity> GetActivitiesPerTimeSpan(RuleTimeSpan timespan)
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

            activities = DataHelper.MergeSameActivities(activities, Settings.MinimumSwitchTimeInSeconds);

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
        #endregion

    }

}