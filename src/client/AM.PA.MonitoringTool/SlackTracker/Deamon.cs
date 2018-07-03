using Shared;
using Shared.Data;
using SlackTracker.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SlackTracker
{
    public sealed class Deamon : BaseTracker, ITracker
    {

        private List<string> _channels;
        private Dictionary<string, string> _channelstoken;
        private bool _wasFirstStart = true;
        private bool _isPApaused = false;

        public Dictionary<string, string> Channelstoken { get => _channelstoken; set => _channelstoken = value; }

        #region Itracker Stuff

        public Deamon ()
        {
            Name = Settings.TRACKER_NAME;
            _channels = new List<String>();
            Channelstoken = new Dictionary<string, string>();
        }

        public override string GetVersion()
        {
            var v = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;
            return Shared.Helpers.VersionHelper.GetFormattedVersion(v);
        }

        public override bool IsEnabled()
        {
            return Database.GetInstance().GetSettingsBool(Settings.TRACKER_ENEABLED_SETTING, Settings.IsEnabledByDefault);
        }

        public override void Start()
        {

        }

        public override void Stop()
        {

        }

        public override void CreateDatabaseTablesIfNotExist()
        {

        }

        public override void UpdateDatabaseTables(int version)
        {

        }
        public void ChangeEnabledState(bool? fibtitTrackerEnabled)
        {
            Console.WriteLine(Settings.TRACKER_NAME + " is now " + (fibtitTrackerEnabled.Value ? "enabled" : "disabled"));
            Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, fibtitTrackerEnabled.Value);
            Database.GetInstance().LogInfo("The participant updated the setting '" + Settings.TRACKER_ENEABLED_SETTING + "' to " + fibtitTrackerEnabled.Value);

            if (fibtitTrackerEnabled.Value && !_isPApaused)
            {
                CreateDatabaseTablesIfNotExist();
                InternalStart();
            }
            else if (!fibtitTrackerEnabled.Value && !_isPApaused && IsRunning)
            {
                InternalStop();
            }
            else
            {
                Logger.WriteToConsole("Don't do anything, tracker is paused");
            }
        }

        public void InternalStart()
        {

        }

        public void InternalStop()
        {

        }

        void OnTrackerDisabled()
        {
            IsRunning = false;
            Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, false);
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
