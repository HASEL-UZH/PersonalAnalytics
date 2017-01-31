using ABB.FocuslightApp.Clients;
using FocusLightTracker.Service;
using Shared;
using Shared.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FlowLightTracker
{
    public class Daemon : BaseTracker, ITracker
    {
        #region FIELDS
        private bool _flowLightTrackerEnabled;
        private FocusLightTracker.Daemon _flowTracker;
        private Timer _updateTimer;
        private bool _locked;
        private bool _enforcing;
        private Timer _enforcingTimer;
        private Blink1Client _lightClient;
        private LyncStatus _skypeClient;
        private Status _currentSkypeStatus;
        private FocusState _currentFlowState;
        #endregion

        #region METHODS
        public Daemon(FocusLightTracker.Daemon flowTracker)
        {
            Name = "FlowLight Tracker";
            _flowTracker = flowTracker;
            _lightClient = new Blink1Client();
            _skypeClient = new LyncStatus();
        }
        public override void CreateDatabaseTablesIfNotExist()
        {
            //TODO: Add a table that logs the status changes and the source, also always log the current color of the light
        }

        public override string GetVersion()
        {
            var v = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;
            return Shared.Helpers.VersionHelper.GetFormattedVersion(v);
        }

        public override bool IsEnabled()
        {
            return FlowLightTrackerEnabled;
        }

        public bool FlowLightTrackerEnabled
        {
            get
            {
                _flowLightTrackerEnabled = Database.GetInstance().GetSettingsBool("FlowLightTrackerEnabled", Settings.IsEnabledByDefault);
                return _flowLightTrackerEnabled;
            }
            set
            {
                var updatedIsEnabled = value;

                // only update if settings changed
                if (updatedIsEnabled == _flowLightTrackerEnabled) return;

                // update settings
                Database.GetInstance().SetSettings("FlowLightTrackerEnabled", value);

                // start/stop tracker if necessary
                if (!updatedIsEnabled && IsRunning)
                {
                    Stop();
                }
                else if (updatedIsEnabled && !IsRunning)
                {
                    Start();
                }

                // log
                Database.GetInstance().LogInfo("The participant updated the setting 'FlowLightTrackerEnabled' to " + updatedIsEnabled);
            }
        }

        public bool IsLocked { get; set; }
        public bool IsEnforced { get; set; }

        public override void Start()
        {
            //register update timer
            if (_updateTimer != null)
                Stop();
            _updateTimer = new Timer();
            _updateTimer.Interval = Settings.UpdateInterval * 1000;
            _updateTimer.Elapsed += _updateTimer_Elapsed;
            _updateTimer.Start();

            IsRunning = true;
        }

        private void _updateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            FocusState newFlowStatus = _flowTracker.GetFocusState();
            setStatus(newFlowStatus);
        }

        private void setStatus(Status newStatus)
        {
            _currentSkypeStatus = newStatus;
            _lightClient.Status = newStatus;
            _skypeClient.Status = newStatus;
        }

        private void setStatus(FocusState newStatus)
        {
            switch (newStatus)
            {
                case FocusState.Low:
                case FocusState.Medium:
                    setStatus(Status.Free);
                    break;
                case FocusState.High:
                    setStatus(Status.Busy);
                    break;
                case FocusState.VeryHigh:
                    setStatus(Status.DoNotDisturb);
                    break;
            }
        }

        public override void Stop()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer.Dispose();
                _updateTimer = null;
            }

            IsRunning = false;
        }

        public override void UpdateDatabaseTables(int version)
        {
            // not needed yet
        }

        #endregion
    }
}
