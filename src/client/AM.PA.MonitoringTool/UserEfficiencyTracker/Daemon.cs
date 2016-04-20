// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.Data;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Shared;
using Shared.Data;
using UserEfficiencyTracker.Models;
using UserEfficiencyTracker.Data;
using System.Collections.Generic;
using UserEfficiencyTracker.Visualizations;
using System.Globalization;

namespace UserEfficiencyTracker
{
    public class Daemon : BaseTracker, ITracker
    {
        private DispatcherTimer _timer;
        private static TimeSpan _timeRemainingUntilNextSurvey;
        private SurveyEntry _currentSurveyEntry;

        #region METHODS

        #region ITracker Stuff

        public Daemon()
        {
            Name = "User Efficiency Survey";
            _timeRemainingUntilNextSurvey = PopUpIntervalInMins;
        }

        public override void Start()
        {
            if (_timer != null)
                Stop();
            _timer = new DispatcherTimer();
            _timer.Interval = Settings.SurveyCheckerInterval;
            _timer.Tick += TimerTick;

            // only start timer if user wants it
            if (PopUpEnabled)
            {
                _timer.Start();
            }

            IsRunning = true;
        }

        public override void Stop()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }

            IsRunning = false;
        }

        public override List<IVisualization> GetVisualizationsDay(DateTimeOffset date)
        {
            var vis1 = new DayProductivityTimeLine(date);
            var vis2 = new DayWeekProductivityProgramsTable(date, VisType.Day);
            return new List<IVisualization> { vis1, vis2 };
        }

        public override List<IVisualization> GetVisualizationsWeek(DateTimeOffset date)
        {
            var vis1 = new WeekProductivityBarChart(date);
            var vis2 = new DayWeekProductivityProgramsTable(date, VisType.Week);
            return new List<IVisualization> { vis1, vis2 };
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            Queries.CreateUserEfficiencyTables();
        }

        public override string GetStatus()
        {
            var nextSurveyTs = DateTime.Now.Add(_timeRemainingUntilNextSurvey);
            return (! IsRunning || ! _timer.IsEnabled)
                ? Name + " is NOT running"
                : Name + " is running. Next mini-survey at " + nextSurveyTs.ToShortDateString() + " " + nextSurveyTs.ToShortTimeString() + ".";
        }

        public override bool IsEnabled()
        {
            return true; // currently, it is always enabled
        }

        #region Settings

        private bool _popUpIsEnabled;
        public bool PopUpEnabled
        {
            get
            {
                _popUpIsEnabled = Database.GetInstance().GetSettingsBool("PopUpEnabled", Settings.DefaultPopUpIsEnabled);
                return _popUpIsEnabled;
            }
            set
            {
                var updatedIsEnabled = value;

                // only update if settings changed
                if (updatedIsEnabled == _popUpIsEnabled) return;
                _popUpIsEnabled = updatedIsEnabled;

                // update settings
                Database.GetInstance().SetSettings("PopUpEnabled", value);

                // start/stop timer if necessary
                if (!updatedIsEnabled && _timer.IsEnabled)
                {
                    _timer.Stop();
                    _popUpIntervalInMins = TimeSpan.MinValue;
                }
                else if (updatedIsEnabled && !_timer.IsEnabled)
                {
                    _timer.Start();
                    _popUpIntervalInMins = PopUpIntervalInMins;
                }

                // log
                Database.GetInstance().LogInfo("The participant updated the setting 'PopUpEnabled' to " + updatedIsEnabled);
            }
        }

        private TimeSpan _popUpIntervalInMins;
        public TimeSpan PopUpIntervalInMins
        {
            get
            {
                var value = Database.GetInstance().GetSettingsInt("PopUpInterval", Settings.DefaultPopUpInterval);
                _popUpIntervalInMins = TimeSpan.FromMinutes(value);
                return _popUpIntervalInMins;
            }
            set
            {
                var updatedInterval = value;

                // only update if settings changed
                if (updatedInterval == _popUpIntervalInMins) return;
                _popUpIntervalInMins = updatedInterval;

                // update settings
                Database.GetInstance().SetSettings("PopUpInterval", updatedInterval.TotalMinutes.ToString(CultureInfo.InvariantCulture));

                // update interval time
                _timeRemainingUntilNextSurvey = _popUpIntervalInMins;

                // log
                Database.GetInstance().LogInfo("The participant updated the setting 'PopUpInterval' to " + _popUpIntervalInMins);
            }
        }

        #endregion

        #endregion

        #region Daemon

        /// <summary>
        /// loop runs in a separate thread
        /// </summary>
        private void TimerTick(object sender, EventArgs args)
        {
            // only show survey when its ready to be shown
            if (_timeRemainingUntilNextSurvey > Settings.SurveyCheckerInterval)
            {
              _timeRemainingUntilNextSurvey = _timeRemainingUntilNextSurvey.Subtract(Settings.SurveyCheckerInterval);
                return;
            }

            // show survey
            RunSurvey(DateTime.Now);
        }

        /// <summary>
        /// runs the survey and handles the response
        /// </summary>
        /// <returns></returns>
        private void RunSurvey(DateTime notifyTimeStamp)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(
                () =>
                {
                    // set previous entry to show previous entry time in popup
                    var previousSurveyEntry = Queries.GetPreviousSurveyEntry();

                    var popup = new UserSurveyNotification(previousSurveyEntry);

                    // first time the notification is shown
                    if (_currentSurveyEntry == null)
                    {
                        _currentSurveyEntry = new SurveyEntry();
                        _currentSurveyEntry.TimeStampNotification = DateTime.Now;
                    }

                    // (re-)set the timestamp of filling out the survey
                    _currentSurveyEntry.TimeStampStarted = DateTime.Now;

                    // show interval survey
                    var response = popup.ShowDialog();

                    // handle response
                    if (response == true)
                    {
                        // user took the survey
                        if (popup.UserSelectedProductivity >= 1 && popup.UserSelectedProductivity <= 7)
                        {
                            SaveSurvey(popup);
                            Database.GetInstance().LogInfo("The participant completed the survey.");
                        }
                        // user didn't work
                        else if (popup.UserSelectedProductivity == -1)
                        {
                            SaveSurvey(popup);
                            Database.GetInstance().LogInfo("The participant didn't work when the pop-up was shown");
                        }
                        // user postponed the survey
                        else if (popup.PostPoneSurvey != PostPoneSurvey.None)
                        {
                            PostponeSurvey(popup);
                            Database.GetInstance().LogInfo(string.Format(CultureInfo.InvariantCulture, "The participant postponed the survey ({0}).", popup.PostPoneSurvey));
                        }
                        // something strange happened
                        else
                        {
                            _currentSurveyEntry = null;
                            _timeRemainingUntilNextSurvey = Settings.IntervalPostponeShortInterval;
                        }
                    }
                    else
                    {
                        //TODO: what happens here?
                        Database.GetInstance().LogErrorUnknown(Name);
                    }
                }));
            }
            catch (ThreadAbortException e) { Database.GetInstance().LogError(Name + ": " + e.Message); }
            catch (Exception e) { Database.GetInstance().LogError(Name + ": " + e.Message); }
        }

        /// <summary>
        /// Saves the survey in the db & resets some items
        /// </summary>
        /// <param name="popup"></param>
        private void SaveSurvey(UserSurveyNotification popup)
        {
            _timeRemainingUntilNextSurvey = PopUpIntervalInMins; // set new default interval

            _currentSurveyEntry.Productivity = popup.UserSelectedProductivity;
            _currentSurveyEntry.TimeStampFinished = DateTime.Now;
            Queries.SaveEntry(_currentSurveyEntry);
            _currentSurveyEntry = null; // reset
        }

        /// <summary>
        /// handler to postpone the survey for the selected time
        /// Hint: the selected time (e.g. postpone 1 hour) equals 1 hour of computer running (i.e. developer working) time
        /// </summary>
        /// <param name="notify"></param>
        private void PostponeSurvey(UserSurveyNotification notify)
        {
            switch (notify.PostPoneSurvey)
            {
                case (PostPoneSurvey.Postpone1):
                    _timeRemainingUntilNextSurvey = Settings.IntervalPostponeShortInterval;  // set new interval
                    break;
                case (PostPoneSurvey.Postpone2):
                    _timeRemainingUntilNextSurvey = TimeSpan.FromHours(1); // in one hour
                    break;
                case (PostPoneSurvey.Postpone3):
                    _timeRemainingUntilNextSurvey = TimeSpan.FromHours(6); // in one workday
                    //var now = DateTime.Now;
                    //var nextDay = now.AddDays(1).Date.AddHours(8); //next day at 8 o'clock
                    //var totalHours = (nextDay - now).TotalHours;
                    //_timeRemainingUntilNextSurvey = TimeSpan.FromHours(totalHours);
                    break;
                default:
                    _timeRemainingUntilNextSurvey = Settings.IntervalPostponeShortInterval;  // set new interval
                    break;
            }
        }

        /// <summary>
        /// manually run survey (click on ContextMenu)
        /// </summary>
        public void ManualTakeSurveyNow()
        {
            RunSurvey(DateTime.Now);
        }
        #endregion

        #endregion
    }
}
