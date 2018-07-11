// Created by Rohit Kaushik (f20150115@goa.bits-pilani.ac.in) at the University of Zurich
// Created: 2018-07-07
// 
// Licensed under the MIT License.

using Shared;
using Shared.Data;
using SlackTracker.Data;
using SlackTracker.Views;
using System;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SlackTracker.Data.SlackModel;

namespace SlackTracker
{
    public sealed class Daemon : BaseTracker, ITracker
    {
        private Window _browserWindow;
        private Timer _slackTimer;
        private bool _isPApaused = false;
        private bool _wasFirstStart = true;

        #region Itracker Stuff

        public Daemon ()
        {
            Name = Settings.TRACKER_NAME;

            SlackConnector.TokenRevoked += SlackConnector_TokenRevoked;
        }

        private void SlackConnector_TokenRevoked()
        {
            Stop();
            Database.GetInstance().SetSettings(Settings.TRACKER_ENABLED_SETTING, false);
        }


        //Checks whether a token is stored. If not, new tokens are retrieved from fitbit
        private void CheckIfTokenIsAvailable()
        {
            if (SecretStorage.GetAccessToken() == null)
            {
                GetNewTokens();
            }
            else
            {
                Logger.WriteToConsole("No need to fetch tokens");
            }
        }

        //Gets new tokens from slack
        internal void GetNewTokens()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                var browser = new EmbeddedBrowser(Settings.REGISTRATION_URL);
                browser.FinishEvent += Browser_FinishEvent;
                browser.RegistrationTokenEvent += Browser_RegistrationTokenEvent;
                browser.ErrorEvent += Browser_ErrorEvent;

                _browserWindow = new Window
                {
                    Title = "Register PersonalAnalytics to let it access Slack data",
                    Content = browser
                };

                _browserWindow.ShowDialog();
            }));
        }

        private void CheckIfSecretsAreAvailable()
        {
            //Check if credentials are there. If not, we get them from the server.
            //Also check if credentials are meaningful or just dummy credentials. We had
            //incidents where we stored a, b, or c as dummy credentials. In this case, the
            //following check should detect these dummy credentials and replace them with the real ones.
            if (SecretStorage.GetSlackClientID() == null ||
                SecretStorage.GetSlackClientSecret() == null ||
                SecretStorage.GetSlackClientID().Length <= 1 ||
                SecretStorage.GetSlackClientSecret().Length <= 1)
            {
                try
                {
                    AccessDataService.AccessDataClient client = new AccessDataService.AccessDataClient();

                    //client.GetSlackClientID();
                    string clientID = "12830536055.392728377956";
                    if (clientID != null)
                    {
                        SecretStorage.SaveSlackClientID(clientID);
                    }

                    //client.GetSlackClientSecret();
                    string clientSecret = "065f3a7b157bb73682366f0fe275da7f";
                    if (clientSecret != null)
                    {
                        SecretStorage.SaveSlackClientSecret(clientSecret);
                    }
                }

                catch (Exception e)
                {
                    Logger.WriteToLogFile(e);
                }
            }
        }

        public override string GetVersion()
        {
            var v = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;
            return Shared.Helpers.VersionHelper.GetFormattedVersion(v);
        }

        public override bool IsEnabled()
        {
            return Database.GetInstance().GetSettingsBool(Settings.TRACKER_ENABLED_SETTING, Settings.IsEnabledByDefault);
        }

        public override bool IsFirstStart { get { _wasFirstStart = !Database.GetInstance().HasSetting(Settings.TRACKER_ENABLED_SETTING); return !Database.GetInstance().HasSetting(Settings.TRACKER_ENABLED_SETTING); } }


        public void InternalStart()
        {
            try
            {
                CheckIfSecretsAreAvailable();
                CheckIfTokenIsAvailable();

                if (IsEnabled())
                {
                    Logger.WriteToConsole("Start Slack Tracker");
                    CreateSlackPullTimer();
                    IsRunning = true;
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        public override void Start()
        {
            _isPApaused = false;
            Logger.WriteToConsole(SecretStorage.GetAccessToken());
            InternalStart();
        }

        public void InternalStop()
        {
            if (_slackTimer != null)
            {
                _slackTimer.Enabled = false;
            }
            IsRunning = false;
        }

        public override void Stop()
        {
            _isPApaused = true;
            InternalStop();
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            DatabaseConnector.CreateSlackTables();
        }

        public override void UpdateDatabaseTables(int version)
        {

        }

        //Called when new data should be pull from the slack API
        private void OnPullFromSlack(object sender, ElapsedEventArgs eventArgs)
        {
            _slackTimer.Interval = Settings.SYNCHRONIZE_INTERVAL;

            Logger.WriteToConsole("Try to sync with Slack");

            try
            {
                DateTimeOffset latestSync;

                if (DatabaseConnector.GetLastTimeSynced() == "never")
                {
                    Logger.WriteToConsole("Sync for the First Time with slack");
                    latestSync = DateTimeOffset.MinValue;
                }
                else
                {
                    latestSync = DateTimeOffset.Parse(DatabaseConnector.GetLastTimeSynced());
                }

                Logger.WriteToConsole("Latest sync date: " + latestSync.ToString(Settings.FORMAT_DAY_AND_TIME));
                Database.GetInstance().SetSettings(Settings.LAST_SYNCED_DATE, DateTimeOffset.Now.ToString(Settings.FORMAT_DAY_AND_TIME));
                //latestSync = latestSync.AddDays(-1);

                GetChannels(latestSync);
                GetLogs(latestSync);
                GetUsers(latestSync);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        private void GetLogs(DateTimeOffset _latestSync)
        {
            List<Log> logs = SlackConnector.Get_Logs(_latestSync);

            foreach (Log l in logs)
            {
                Logger.WriteToConsole(l.channel_id + " : " + l.message);
            }

            DatabaseConnector.SaveLogs(logs);
        }

        private void GetUsers(DateTimeOffset _latestSync)
        {
            List<User> users = SlackConnector.GetUsers();

            foreach (User u in users)
            {
                Logger.WriteToConsole(u.name);
            }

            DatabaseConnector.SaveUsers(users);
        }

        private void GetChannels(DateTimeOffset _latestSync)
        {
            List<Channel> channels = SlackConnector.GetChannels();
            foreach (Channel c in channels)
            {
                Logger.WriteToConsole(c.name);
            }

            DatabaseConnector.SaveChannels(channels);
        }

        //Creates a timer that is used to periodically pull data from the slack API
        private void CreateSlackPullTimer()
        {
            _slackTimer = new Timer();
            _slackTimer.Elapsed += OnPullFromSlack;
            _slackTimer.Interval = Settings.SYNCHRONIZE_INTERVAL;
            _slackTimer.Enabled = true;
        }

        public void ChangeEnabledState(bool? slackTrackerEnabled)
        {
            Console.WriteLine(Settings.TRACKER_NAME + " is now " + (slackTrackerEnabled.Value ? "enabled" : "disabled"));
            Database.GetInstance().SetSettings(Settings.TRACKER_ENABLED_SETTING, slackTrackerEnabled.Value);
            Database.GetInstance().LogInfo("The participant updated the setting '" + Settings.TRACKER_ENABLED_SETTING + "' to " + slackTrackerEnabled.Value);

            if (slackTrackerEnabled.Value && !_isPApaused)
            {
                CreateDatabaseTablesIfNotExist();
                InternalStart();
            }
            else if (!slackTrackerEnabled.Value && !_isPApaused && IsRunning)
            {
                InternalStop();
            }
            else
            {
                Logger.WriteToConsole("Don't do anything, tracker is paused");
            }
        }

        void OnTrackerDisabled()
        {
            IsRunning = false;
            Database.GetInstance().SetSettings(Settings.TRACKER_ENABLED_SETTING, false);
        }

        //Called when new tokens were received from slack
        private void Browser_RegistrationTokenEvent(string token)
        {
            CheckIfSecretsAreAvailable();
            SlackConnector.GetAccessToken(token);
        }

        private void Browser_ErrorEvent()
        {
            throw new NotImplementedException();
        }

        //Called when the browser window used to retrieve tokens from fitbit should be closed
        private void Browser_FinishEvent()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                _browserWindow.Close();
            }));
        }

        public override List<IVisualization> GetVisualizationsDay(DateTimeOffset date)
        {
            return new List<IVisualization> { new SlackVisualizationForDay(date) };
        }

        public override List<IVisualization> GetVisualizationsWeek(DateTimeOffset date)
        {
            return null;
            // Implement
        }

        public override List<IFirstStartScreen> GetStartScreens()
        {
            return new List<IFirstStartScreen>() { new FirstStartWindow() };
        }
        #endregion

        #region Events and Helpers

        #endregion

    }
}
