using ABB.FocuslightApp.Clients;
using FocusLightTracker.Service;
using Microsoft.Win32;
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
        enum Originator {System, Skype, FlowTracker };

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

        public override void Start()
        {
            //register and start update timer (for FlowTracker)
            if (_updateTimer != null)
                Stop();
            _updateTimer = new Timer();
            _updateTimer.Interval = Settings.UpdateInterval * 1000;
            _updateTimer.Elapsed += UpdateTimer_Elapsed;
            _updateTimer.Start();

            //register event handler for status changes in skype
            _skypeClient.OnOutsideChange += SkypeClient_OnOutsideChange;

            //register enforcing timer (for manual state changes)
            _enforcingTimer = new Timer();
            _enforcingTimer.Elapsed += EnforcingTimer_Elapsed;

            //register event to track when work station is locked / unlocked
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;

            IsRunning = true;
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {                
                setStatus(Originator.System, Status.Away);
                _locked = true;
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                setStatus(Originator.System, Status.Free);
                _locked = false;
            }
        }

        private void EnforcingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            StopEnforcing();
        }

        private void SkypeClient_OnOutsideChange(object sender, StatusEventArgs e)
        {
            Console.WriteLine("change from skype: " + e.CurrentStatus + ", " + e.Outside);

            // Ignore all changes from Skype if the workstation is locked. 
            // Reason: Sometimes Skype cannot reflect the correct status (Away) when the computer is locked.
            if (!_locked)
            {
                if (_enforcing)
                {
                    // if the state has been enforced and changed again now, we cancel the enforcing
                    StopEnforcing();
                }

                if (e.CurrentStatus == Status.Free)
                {
                    // if the status was set to free (manually or by the calendar),
                    // we will respect that for 5 minutes and then start changing the state again by FlowTracker
                    StartTimedEnforcing(5);
                } 
                else
                {
                    StartInfiniteEnforcing();
                }

                setStatus(Originator.Skype, e.CurrentStatus);           
            }
        }

        private void StartTimedEnforcing(int minutes)
        {
            Console.WriteLine("Enforcing for " + minutes + ".");
            _enforcing = true;
            _enforcingTimer.Stop();
            _enforcingTimer.Interval = minutes * 60 * 1000;
            _enforcingTimer.Start();
        }

        private void StartInfiniteEnforcing()
        {
            Console.WriteLine("Enforcing forever.");
            _enforcing = true;
        }

        private void StopEnforcing()
        {
            Console.WriteLine("Cancelling enforcing.");

            _enforcingTimer.Stop();
            _enforcing = false;
        }

        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("Updating ...");

            // Don't do anything if the work station is locked or we are enforcing a state
            if (!_locked && !_enforcing)
            {
                // set the status to the one determined by FlowTracker
                FocusState newFlowStatus = _flowTracker.GetFocusState();
                setStatusFromFlowTracker(newFlowStatus);

                Console.WriteLine("Update: set status to " + newFlowStatus);
            }
        }

        private void setStatus(Originator originator, Status newStatus)
        {
            if (originator != Originator.Skype)
            {
                // Skype should only be updated if it is origniated by FlowTracker, otherwise it is already updated
                _skypeClient.Status = newStatus;      
            }

            _currentSkypeStatus = newStatus;
            _lightClient.Status = newStatus;
        }

        private void setStatusFromFlowTracker(FocusState newStatus)
        {
            switch (newStatus)
            {
                case FocusState.Low:
                case FocusState.Medium:
                    setStatus(Originator.FlowTracker, Status.Free);
                    break;
                case FocusState.High:
                    setStatus(Originator.FlowTracker, Status.Busy);
                    break;
                case FocusState.VeryHigh:
                    setStatus(Originator.FlowTracker, Status.DoNotDisturb);
                    break;
            }

            _currentFlowState = newStatus;
        }

        public override void Stop()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer.Dispose();
                _updateTimer = null;
            }

            _skypeClient.OnOutsideChange -= SkypeClient_OnOutsideChange;

            IsRunning = false;
        }

        public override void UpdateDatabaseTables(int version)
        {
            // not needed yet
        }

        #endregion
    }
}
