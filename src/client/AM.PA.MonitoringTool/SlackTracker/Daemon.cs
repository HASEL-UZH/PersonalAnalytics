using Shared;
using Shared.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SlackTracker
{
    public sealed class Daemon : BaseTracker, ITracker
    {
        #region Itracker Stuff

        public Daemon ()
        {
            Name = Settings.Name;
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
            // Implemenet
        }

        public override List<IFirstStartScreen> GetStartScreens()
        {
            return null;
            //return new List<IFirstStartScreen>() { new FirstStartWindow() };
        }
        #endregion

        #region Events and Helpers

        #endregion
    }
}
