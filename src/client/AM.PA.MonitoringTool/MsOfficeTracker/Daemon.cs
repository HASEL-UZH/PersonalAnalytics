// Created by André Meyer at MSR
// Created: 2015-12-07
// 
// Licensed under the MIT License.

using System;
using Shared;
using System.Collections.Generic;
using System.Windows;
using MsOfficeTracker.Visualizations;
using MsOfficeTracker.Data;
using Shared.Data;
using System.Globalization;
using System.Threading;
using MsOfficeTracker.Helpers;
using System.Reflection;
using System.Windows.Controls;
using MsOfficeTracker.Views;

namespace MsOfficeTracker
{
    public class Daemon : BaseTracker, ITracker
    {
        private Timer _timer;
        private DateTime _lastDayEntry;

        #region ITracker Stuff

        public Daemon()
        {
            Name = "Office 365 Tracker";
        }

        public async override void Start()
        {
            // initialize API & authenticate if necessary
            var isAuthenticated = await Office365Api.GetInstance().Authenticate();

            // disable tracker if authentication was without success
            if (!isAuthenticated)
            {
                IsRunning = false;

                var msg = string.Format(CultureInfo.InvariantCulture, "The {0} was disabled as the authentication with Office 365 failed. Maybe you don't have an internet connection or the Office 365 credentials were wrong.\n\nThe tool will prompt the Office 365 login again with the next start of the application. You can also disable the {0} in the settings.\n\nIf the problem persists, please contact us via " + Shared.Settings.EmailAddress1 + " and attach the logfile.", Name);
                MessageBox.Show(msg, Dict.ToolName + ": Error", MessageBoxButton.OK); //todo: use toast message
                return;
            }
            else
            {
                IsRunning = true;
            }

            // Start Email Count Timer
            if (_timer != null)
                Stop();

            // initialize a new timer
            var interval = (int)TimeSpan.FromMinutes(Settings.SaveEmailCountsIntervalInMinutes).TotalMilliseconds;
            _timer = new Timer(new TimerCallback(TimerTick), // callback
                            null,  // no idea
                            60000, // start immediately after 1 minute
                            interval); // interval
        }

        public override void Stop()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }

            IsRunning = false;
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            Queries.CreateMsTrackerTables();
        }

        public override void UpdateDatabaseTables(int version)
        {
            Queries.UpdateDatabaseTables(version);
        }
        
        public override bool IsFirstStart { get { return !Database.GetInstance().HasSetting("MsOfficeTrackerEnabled"); } }

        public override List<FirstStartScreenContainer> GetStartScreens()
        {
            FirstStartScreen window = new FirstStartScreen();
            return new List<FirstStartScreenContainer>() {new FirstStartScreenContainer(window, this.Name, window.NextClicked)};
        }

        public override List<IVisualization> GetVisualizationsDay(DateTimeOffset date)
        {
            //var vis1 = new DayEmailsSent(date);
            //var vis2 = new DayEmailsReceived(date);
            //var vis3 = new DayEmailsAvgUnreadInbox(date);
            var vis = new DayEmailsTable(date);
            return new List<IVisualization> { vis };
        }

        public override bool IsEnabled()
        {
            return MsOfficeTrackerEnabled;
        }

        public override string GetVersion()
        {
            var v = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;
            return Shared.Helpers.VersionHelper.GetFormattedVersion(v);
        }

        private bool _msOfficeTrackerEnabled;
        public bool MsOfficeTrackerEnabled
        {
            get
            {
                _msOfficeTrackerEnabled = Database.GetInstance().GetSettingsBool("MsOfficeTrackerEnabled", Settings.IsEnabledByDefault);
                return _msOfficeTrackerEnabled;
            }
            set
            {
                var updatedIsEnabled = value;

                // only update if settings changed
                if (updatedIsEnabled == _msOfficeTrackerEnabled) return;

                // update settings
                Database.GetInstance().SetSettings("MsOfficeTrackerEnabled", updatedIsEnabled);

                // start/stop tracker if necessary
                if (!updatedIsEnabled && IsRunning)
                {
                    Stop();
                }
                else if (updatedIsEnabled && !IsRunning)
                {
                    CreateDatabaseTablesIfNotExist();
                    Start();
                }

                // if tracker was disabled out, sign out and clear cache
                if (updatedIsEnabled == false)
                {
                    Office365Api.GetInstance().SignOut();
                }

                // log
                Database.GetInstance().LogInfo("The participant updated the setting 'MsOfficeTrackerEnabled' to " + updatedIsEnabled);
            }
        }

        #endregion

        #region Daemon

        private void TimerTick(object state)
        {
            var now = DateTime.Now;

            // save email infos & meetings infos for current timestamp
            SaveEmailsCount(now);
            SaveMeetingsCount(now);

            // go a few days back to cache email & meeting data (if necessary)
            SaveDaysBeforeCounts();
        }

        /// <summary>
        /// Regularly runs and saves some email counts
        /// </summary>
        private void SaveEmailsCount(DateTime date)
        {
            try
            {
                // don't do it if already done for the date; always check for the current date
                if (date.Date != DateTime.Now.Date && Queries.HasEmailsEntriesForDate(date, true)) return;

                // create and save a new email snapshot (inbox, sent, received)
                Queries.CreateEmailsSnapshot(date, true);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Regularly runs and saves some email counts
        /// </summary>
        private void SaveMeetingsCount(DateTimeOffset date)
        {
            try
            {
                // don't do it if already done for the date; always check for the current date
                if (date.Date != DateTime.Now.Date // always do it for current day
                    && Queries.HasMeetingEntriesForDate(date)) // only do it for past day, if there are no entries
                    return;

                // get meetings for the date
                var meetingsResult = Office365Api.GetInstance().LoadMeetings(date);
                meetingsResult.Wait();
                var meetings = meetingsResult.Result;

                if (meetings.Count > 0)
                {
                    // delete old entries (to add updated meetings)
                    Queries.RemoveMeetingsForDate(date);

                    // save new meetings into the database
                    foreach (var meeting in meetings)
                    {
                        var duration = (int)Math.Round(Math.Abs((meeting.End - meeting.Start).TotalMinutes), 0);
                        if (duration >= 24 * 60) continue; // only store if not multiple-day meeting
                        var start = meeting.Start.ToLocalTime();
                        Queries.SaveMeetingsSnapshot(start, meeting.Subject, duration);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Once a day, save yesterday's (and some other days before) meetings and emails
        /// </summary>
        private void SaveDaysBeforeCounts()
        {
            try
            {
                // only save yesterday counts if NOT already done (more efficient to not waste resources!)
                // if it was yesterday, the other days are probably in
                var yesterday = DateTime.Now.AddDays(-1);
                if (_lastDayEntry.Date == yesterday.Date) return;

                // so, now we are good for yesterday and some days before
                _lastDayEntry = yesterday;

                // go 'UpdateCacheForDays' back and update the cache
                for (var day = 1; day <= Settings.UpdateCacheForDays; day++)
                {
                    var date = DateTime.Now.AddDays(-day);
                    SaveEmailsCount(date);
                    SaveMeetingsCount(date);
                }  
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        #endregion
    }
}
