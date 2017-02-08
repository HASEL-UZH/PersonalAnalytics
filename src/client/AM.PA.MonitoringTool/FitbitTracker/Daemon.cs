// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-23
// 
// Licensed under the MIT License.

using Shared;
using Shared.Data;
using System;
using System.Timers;
using FitbitTracker.Data;
using FitbitTracker.Model;
using System.Collections.Generic;
using FitbitTracker.Data.FitbitModel;
using FitbitTracker.View;
using System.Windows;
using System.Reflection;

namespace FitbitTracker
{
    public sealed class Deamon : BaseTracker, ITracker
    {
        private Window browserWindow;
        private Timer fitbitTimer;

        public Deamon()
        {
            Name = Settings.TRACKER_NAME;
            if (Settings.IsDetailedCollectionAvailable)
            {
                Name += " (detailed)";
            }
        }

        public override string GetVersion()
        {
            var v = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;
            return Shared.Helpers.VersionHelper.GetFormattedVersion(v);
        }

        public override string GetStatus()
        {
            return IsRunning ? (Name + " is running.") : (Name + " is NOT running.");
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            DatabaseConnector.CreateFitbitTables();
        }

        public override bool IsEnabled()
        {
            return Database.GetInstance().GetSettingsBool(Settings.TRACKER_ENEABLED_SETTING, true);
        }

        public override void Start()
        {
            bool isFirstStart = !Database.GetInstance().HasSetting(Settings.TRACKER_ENEABLED_SETTING);

            FitbitConnector.RefreshTokenFail += FitbitConnector_RefreshTokenFail;

            if (isFirstStart)
            {
                FirstStartWindow window = new FirstStartWindow();
                window.ErrorEvent += Browser_ErrorEvent;
                window.RegistrationTokenEvent += Browser_RegistrationTokenEvent;
                window.ShowDialog();
            }
            else
            {
                CheckIfTokenIsAvailable();
            }

            Logger.WriteToConsole("Start Fitibit Tracker");
            CreateFitbitPullTimer();
            IsRunning = true;
        }

        //Called whenever refreshing the access or refresh token failed with a not authorized or bad request message
        private void FitbitConnector_RefreshTokenFail()
        {
            Logger.WriteToConsole("Refresh access token failed. Let the user register to get new tokens");
            GetNewTokens();
        }

        //Checks whether a token is stored. If not, new tokens are retrieved from fitbit
        private void CheckIfTokenIsAvailable()
        {
            if (SecretStorage.GetAccessToken() == null || SecretStorage.GetRefreshToken() == null)
            {
                GetNewTokens();
            }
            else
            {
                Logger.WriteToConsole("No need to refresh tokens");
            }
        }

        //Gets new tokens from fitbit
        internal void GetNewTokens()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                EmbeddedBrowser browser = new EmbeddedBrowser(Settings.REGISTRATION_URL);
                browser.FinishEvent += Browser_FinishEvent;
                browser.RegistrationTokenEvent += Browser_RegistrationTokenEvent;
                browser.ErrorEvent += Browser_ErrorEvent;

                browserWindow = new Window
                {
                    Title = "Register PersonalAnalytics to let it access Fitbit data",
                    Content = browser
                };

                browserWindow.ShowDialog();
            }));
        }

        //Called when getting new tokens from fitbit causes an error
        private void Browser_ErrorEvent()
        {
            Logger.WriteToConsole("Couldn't register Fibit. FitbitTracker will be disabled.");
            IsRunning = false;
            Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, false);
        }

        public void ChangeEnabledState(bool? fibtitTrackerEnabled)
        {
            Console.WriteLine(Settings.TRACKER_NAME + " is now " + (fibtitTrackerEnabled.Value ? "enabled" : "disabled"));
            Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, fibtitTrackerEnabled.Value);
            Database.GetInstance().LogInfo("The participant updated the setting '" + Settings.TRACKER_ENEABLED_SETTING + "' to " + fibtitTrackerEnabled.Value);

            if (fibtitTrackerEnabled.Value)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }

        //Called when new tokens were received from fitbit
        private void Browser_RegistrationTokenEvent(string token)
        {
            FitbitConnector.GetFirstAccessToken(token);
        }

        //Called when the browser window used to retrieve tokens from fitbit should be closed
        private void Browser_FinishEvent()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                browserWindow.Close();
            }));
        }

        //Creates a timer that is used to periodically pull data from the fitbit API
        private void CreateFitbitPullTimer()
        {
            fitbitTimer = new Timer();
            fitbitTimer.Elapsed += OnPullFromFitbit;
            fitbitTimer.Interval = Settings.SYNCHRONIZE_INTERVALL_FIRST;
            fitbitTimer.Enabled = true;
        }

        //Called when new data should be pull from the fitbit API
        private void OnPullFromFitbit(object sender, ElapsedEventArgs eventArgs)
        {
            fitbitTimer.Interval = Settings.SYNCHRONIZE_INTERVALL_SECOND;

            Logger.WriteToConsole("Try to sync with Fitbit");

            try
            {
                DateTimeOffset latestSync = FitbitConnector.GetLatestSyncDate();
                if (latestSync == DateTimeOffset.MinValue)
                {
                    Logger.WriteToConsole("Can't sync now. No timestamp received.");
                }
                else
                {
                    Logger.WriteToConsole("Latest sync date: " + latestSync.ToString(Settings.FORMAT_DAY_AND_TIME));
                    Database.GetInstance().SetSettings(Settings.LAST_SYNCED_DATE, latestSync.ToString(Settings.FORMAT_DAY_AND_TIME));
                    latestSync = latestSync.AddDays(-1);

                    GetStepData(latestSync);
                    GetActivityData(latestSync);
                    GetHRData(latestSync);
                    GetSleepData(latestSync);
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        //Retrieves the step data from fitbit
        private static void GetStepData(DateTimeOffset latestSync)
        {
            List<DateTimeOffset> days = DatabaseConnector.GetDaysToSynchronize(DataType.STEPS);

            foreach (DateTimeOffset day in days)
            {
                Logger.WriteToConsole("Sync Steps: " + day.ToString(Settings.FORMAT_DAY));

                if (Settings.IsDetailedCollectionAvailable)
                {
                    StepData stepData = FitbitConnector.GetStepDataForDay(day);
                    DatabaseConnector.SaveStepDataForDay(stepData, day, false);
                }

                StepData aggregatedData = FitbitConnector.GetStepDataAggregatedForDay(day);
                DatabaseConnector.SaveStepDataForDay(aggregatedData, day, true);

                if (day < latestSync)
                {
                    Logger.WriteToConsole("Finished syncing Steps for day: " + day.ToString(Settings.FORMAT_DAY));
                    DatabaseConnector.SetSynchronizedDay(day, DataType.STEPS);
                }
            }
        }

        //Retrieves activity data from fitbit
        private static void GetActivityData(DateTimeOffset latestSync)
        {
            List<DateTimeOffset> days = DatabaseConnector.GetDaysToSynchronize(DataType.ACTIVITIES);

            foreach (DateTimeOffset day in days)
            {
                Logger.WriteToConsole("Sync Activity: " + day.ToString(Settings.FORMAT_DAY));
                ActivityData activityData = FitbitConnector.GetActivityDataForDay(day);
                DatabaseConnector.SaveActivityData(activityData, day);
                if (day < latestSync)
                {
                    Logger.WriteToConsole("Finished syncing Activity for day: " + day.ToString(Settings.FORMAT_DAY));
                    DatabaseConnector.SetSynchronizedDay(day, DataType.ACTIVITIES);
                }
            }
        }

        //Retrieves HR data from fitbit
        private static void GetHRData(DateTimeOffset latestSync)
        {
            List<DateTimeOffset> days = DatabaseConnector.GetDaysToSynchronize(DataType.HR);

            foreach (DateTimeOffset day in days)
            {
                Logger.WriteToConsole("Sync HR: " + day.ToString(Settings.FORMAT_DAY));
                Tuple<List<HeartRateDayData>, List<HeartrateIntraDayData>> hrData = FitbitConnector.GetHeartrateForDay(day);
                DatabaseConnector.SaveHRData(hrData.Item1);

                if (Settings.IsDetailedCollectionAvailable)
                {
                    DatabaseConnector.SaveHRIntradayData(hrData.Item2);
                }

                if (day < latestSync)
                {
                    Logger.WriteToConsole("Finished syncing HR for day: " + day.ToString(Settings.FORMAT_DAY));
                    DatabaseConnector.SetSynchronizedDay(day, DataType.HR);
                }
            }
        }

        //Retrieves sleep data from fitbit
        private static void GetSleepData(DateTimeOffset latestSync)
        {
            List<DateTimeOffset> days = DatabaseConnector.GetDaysToSynchronize(DataType.SLEEP);

            foreach (DateTimeOffset day in days)
            {
                Logger.WriteToConsole("Sync sleep: " + day);
                SleepData sleepData = FitbitConnector.GetSleepDataForDay(day);
                DatabaseConnector.SaveSleepData(sleepData, day);
                if (day < latestSync)
                {
                    Logger.WriteToConsole("Finished syncing sleep for day: " + day);
                    DatabaseConnector.SetSynchronizedDay(day, DataType.SLEEP);
                }
            }
        }

        public override void Stop()
        {
            if (fitbitTimer != null)
            {
                fitbitTimer.Enabled = false;
            }
            IsRunning = false;
        }

        public override void UpdateDatabaseTables(int version)
        {
            //No updates needed so far!
        }

        public override List<IVisualization> GetVisualizationsDay(DateTimeOffset date)
        {
            return new List<IVisualization> { new SleepVisualizationForDay(date), new StepVisualizationForDay(date) };
        }

        public override List<IVisualization> GetVisualizationsWeek(DateTimeOffset date)
        {
            return new List<IVisualization> { new SleepVisualizationForWeek(date), new StepVisualizationForWeek(date) };
        }

    }
}