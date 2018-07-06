using Shared;
using Shared.Data;
using SlackTracker.Data;
using SlackTracker.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SlackTracker
{
    public sealed class Daemon : BaseTracker, ITracker
    {
        private Window _browserWindow;
        private List<string> _channels;
        private Dictionary<string, string> _channelstoken;
        private bool _wasFirstStart = true;
        private bool _isPApaused = false;

        public Dictionary<string, string> Channelstoken { get => _channelstoken; set => _channelstoken = value; }

        #region Itracker Stuff

        public Daemon ()
        {
            Name = Settings.TRACKER_NAME;
            _channels = new List<String>();
            Channelstoken = new Dictionary<string, string>();

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

        //Gets new tokens from fitbit
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

                    string authorizationCode = client.GetFitbitFirstAuthorizationCode();
                    if (authorizationCode != null)
                    {
                        SecretStorage.SaveFitbitFirstAuthorizationCode(authorizationCode);
                    }

                    string clientID = client.GetFitbitClientID();
                    if (clientID != null)
                    {
                        SecretStorage.SaveFitbitClientID(clientID);
                    }

                    string clientSecret = client.GetFitbitClientSecret();
                    if (clientSecret != null)
                    {
                        SecretStorage.SaveFitbitClientSecret(clientSecret);
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

        }

        public override void Start()
        {
            _isPApaused = false;
            InternalStart();
        }

        public void InternalStop()
        {

        }

        public override void Stop()
        {
            _isPApaused = true;
            InternalStop();
        }

        public override void CreateDatabaseTablesIfNotExist()
        {

        }

        public override void UpdateDatabaseTables(int version)
        {

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

        //Called when new tokens were received from fitbit
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
            return null;
            // Implement
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
        
        public void AddChannel (String ch)
        {
            _channels.Add(ch);
        }

        public List<String> GetChannels ()
        {
            return _channels;
        }

        #endregion

    }
}
