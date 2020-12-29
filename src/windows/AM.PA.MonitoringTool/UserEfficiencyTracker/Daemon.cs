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
using System.Reflection;

namespace UserEfficiencyTracker
{
    public enum SurveyMode
    {
        IntervalPopUp,
        DailyPopUp
    }

    public class Daemon : BaseTracker, ITracker
    {
        private DispatcherTimer _timer;
        private static TimeSpan _timeRemainingUntilNextSurvey;
        private SurveyEntry _currentSurveyEntry;
        private DateTime _lastDailyPopUpResponse;

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

        public override void UpdateDatabaseTables(int version)
        {
            // no database updates necessary yet
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
            return PopUpEnabled;
        }

        public override string GetVersion()
        {
            var v = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;
            return Shared.Helpers.VersionHelper.GetFormattedVersion(v);
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
                if (!updatedIsEnabled && IsRunning)
                {
                    Stop();
                    _popUpIntervalInMins = TimeSpan.MinValue;
                }
                else if (updatedIsEnabled && !IsRunning)
                {
                    CreateDatabaseTablesIfNotExist();
                    Start();
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

        #region PopUp Daemon

        /// <summary>
        /// loop runs in a separate thread
        /// </summary>
        private void TimerTick(object sender, EventArgs args)
        {
            CheckAndRunDailySurvey();
            CheckAndRunIntervalSurvey();
        }

        /// <summary>
        /// Opens the daily pop-up in case:
        /// 1. it's later than DailyPopUpEarliestMoment (e.g. 5am, since a person might still be working from the day before)
        /// 2. no pop-up was answered at that day before 
        /// 3. if previous workday was not more than 3 days ago (otherwise, people might not remember it)
        /// </summary>
        private void CheckAndRunDailySurvey()
        {
            if (DateTime.Now.TimeOfDay >= Settings.DailyPopUpEarliestMoment && // not before 05.00 am
                DateTime.Now.Date != _lastDailyPopUpResponse.Date &&  // no pop-up today yet (perf to save on more expensive queries)
                (DateTime.Now.Date - Queries.GetPreviousActiveWorkDay()).TotalDays <= 3 && // only if previous work day was max 3 days ago
                Queries.NotYetRatedProductivityForDate(Queries.GetPreviousActiveWorkDay())) // not yet rated previous work day
            {
                RunSurvey(SurveyMode.DailyPopUp);
                return; // don't immediately show interval survey
            }
        }

        /// <summary>
        /// Opens the interval pop-up in case the time until the next survey is smaller
        /// than the SurveyCheckerInterval.
        /// </summary>
        private void CheckAndRunIntervalSurvey()
        {
            if (_timeRemainingUntilNextSurvey > Settings.SurveyCheckerInterval)
            {
                _timeRemainingUntilNextSurvey = _timeRemainingUntilNextSurvey.Subtract(Settings.SurveyCheckerInterval);
                return; // not necessary
            }
            else
            {
                RunSurvey(SurveyMode.IntervalPopUp);
            }
        }

        /// <summary>
        /// runs the survey and handles the response
        /// </summary>
        /// <returns></returns>
        private void RunSurvey(SurveyMode mode)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(
                () =>
                {
                    var previousActiveWorkday = Queries.GetPreviousActiveWorkDay();

                    // if it's the first time the notification is shown
                    if (_currentSurveyEntry == null)
                    {
                        _currentSurveyEntry = new SurveyEntry();
                        _currentSurveyEntry.TimeStampNotification = DateTime.Now;
                        if (previousActiveWorkday > DateTime.MinValue) _currentSurveyEntry.PreviousWorkDay = previousActiveWorkday;
                    }

                    // (re-)set the timestamp of filling out the survey
                    _currentSurveyEntry.TimeStampStarted = DateTime.Now;

                    // set previous entry to show previous entry time in popup
                    var popup = (mode == SurveyMode.IntervalPopUp)
                        ? (Window)new IntervalProductivityPopUp(Queries.GetPreviousIntervalSurveyEntry())
                        : (Window)new DailyProductivityPopUp(previousActiveWorkday);

                    // show popup & handle response
                    if (mode == SurveyMode.DailyPopUp 
                        && ((DailyProductivityPopUp)popup).ShowDialog() == true)
                    {
                        HandleDailyPopUpResponse((DailyProductivityPopUp)popup);
                    }
                    else if (mode == SurveyMode.IntervalPopUp
                        && ((IntervalProductivityPopUp)popup).ShowDialog() == true)
                    {
                        HandleIntervalPopUpResponse((IntervalProductivityPopUp)popup);
                    }
                    else
                    {
                        // we get here when DialogResult is set to false (which never happens) 
                        Database.GetInstance().LogErrorUnknown(Name);

                        // to ensure it still shows some pop-ups later 
                        _timeRemainingUntilNextSurvey = PopUpIntervalInMins;
                    }
                }));
            }
            catch (ThreadAbortException e) { Database.GetInstance().LogError(Name + ": " + e.Message); }
            catch (Exception e) { Database.GetInstance().LogError(Name + ": " + e.Message); }
        }

        #region Interval Survey Methods

        /// <summary>
        /// handles the response to the interval popup
        /// </summary>
        /// <param name="popup"></param>
        private void HandleIntervalPopUpResponse(IntervalProductivityPopUp popup)
        {
            // user took the survey || user didn't work
            if ((popup.UserSelectedProductivity >= 1 && popup.UserSelectedProductivity <= 7) || popup.UserSelectedProductivity == -1)
            {
                SaveIntervalSurvey(popup);
            }
            // user postponed the survey
            else if (popup.PostPoneSurvey != PostPoneSurvey.None)
            {
                PostponeIntervalSurvey(popup);
                Database.GetInstance().LogInfo(string.Format(CultureInfo.InvariantCulture, "The participant postponed the interval-survey ({0}).", popup.PostPoneSurvey));
            }
            // something strange happened
            else
            {
                _currentSurveyEntry = null;
                _timeRemainingUntilNextSurvey = Settings.IntervalPostponeShortInterval;
            }
        }

        /// <summary>
        /// Saves the interval-survey results in the db & resets some items
        /// </summary>
        /// <param name="popup"></param>
        private void SaveIntervalSurvey(IntervalProductivityPopUp popup)
        {
            _timeRemainingUntilNextSurvey = PopUpIntervalInMins; // set new default interval

            _currentSurveyEntry.Productivity = popup.UserSelectedProductivity;
            _currentSurveyEntry.TimeStampFinished = DateTime.Now;
            Queries.SaveIntervalEntry(_currentSurveyEntry);
            _currentSurveyEntry = null; // reset
        }

        /// <summary>
        /// handler to postpone the survey for the selected time
        /// Hint: the selected time (e.g. postpone 1 hour) equals 1 hour of computer running (i.e. developer working) time
        /// </summary>
        /// <param name="notify"></param>
        private void PostponeIntervalSurvey(IntervalProductivityPopUp notify)
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
            RunSurvey(SurveyMode.IntervalPopUp);
        }

        #endregion

        #region Daily Survey Methods

        /// <summary>
        /// Handles the response to the daily popup
        /// </summary>
        /// <param name="popup"></param>
        private void HandleDailyPopUpResponse(DailyProductivityPopUp popup)
        {
            // user took the survey || user didn't work
            if ((popup.UserSelectedProductivity >= 1 && popup.UserSelectedProductivity <= 7) || popup.UserSelectedProductivity == -1)
            {
                SaveDailySurvey(popup);
            }
            // something strange happened
            else
            {
                _currentSurveyEntry = null;
                _timeRemainingUntilNextSurvey = Settings.IntervalPostponeShortInterval;
            }
        }

        /// <summary>
        /// Saves the daily survey result in the db & resets some items
        /// </summary>
        /// <param name="popup"></param>
        private void SaveDailySurvey(DailyProductivityPopUp popup)
        {
            _lastDailyPopUpResponse = DateTime.Now.Date; // no more daily pop-up for today

            _currentSurveyEntry.Productivity = popup.UserSelectedProductivity;
            _currentSurveyEntry.TimeStampFinished = DateTime.Now;
            Queries.SaveDailyEntry(_currentSurveyEntry);
            _currentSurveyEntry = null; // reset
        }

        #endregion

        #endregion

        #endregion
    }
}
