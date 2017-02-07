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
        enum Originator {System, Skype, FlowTracker, User };

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
        private bool _updating;

        #region ITracker Stuff

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

        public override void UpdateDatabaseTables(int version)
        {
            // not needed yet
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

        #endregion

        #region Daemon

        /// <summary>
        /// is run when the update timer elapsed and sets the status according to 
        /// Flowtracker, unless the computer is locked, or we are enforcing a certain state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Don't do anything if the work station is locked or we are enforcing a state
            if (!_locked && !_enforcing)
            {
                // set the status to the one determined by FlowTracker
                FocusState newFlowStatus = _flowTracker.GetFocusState();
                setStatus(newFlowStatus);

                Logger.WriteToConsole("FlowLight: Updating from FlowTracker to " + newFlowStatus);
            }
        }

        /// <summary>
        /// This method is executed whenever the status was changed in Skype.
        /// This could be caused by the user (manual change) or automatically by the calender,
        /// if there is an event starting or ending.
        /// 
        /// These status changes override the status if it has been enforced before. If the
        /// status is set to anything but Free, this will be respected and enforced infinitely.
        /// 
        /// if the status is set back to Free, this will now be enforced for 5 minutes until
        /// FlowTracker can start to change the status again.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SkypeClient_OnOutsideChange(object sender, StatusEventArgs e)
        {
            Logger.WriteToConsole("FlowLight: Skype status change detected: " + e.CurrentStatus + ", outside: " + e.Outside);

            // Ignore all changes from Skype if the workstation is locked. 
            // Reason: Sometimes Skype cannot reflect the correct status (Away) when the computer is locked.
            if (!_locked && !e.Outside)
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

        /// <summary>
        /// sets the status of the light and skype
        /// </summary>
        /// <param name="originator"></param>
        /// <param name="newStatus"></param>
        private void setStatus(Originator originator, Status newStatus)
        {
            _updating = true;

            if (originator != Originator.Skype)
            {
                // Skype should only be updated if it is origniated by FlowTracker, otherwise it is already updated
                _skypeClient.Status = newStatus;
            }

            _currentSkypeStatus = newStatus;
            _lightClient.Status = newStatus;

            _updating = false;
        }

        /// <summary>
        /// sets the status from FlowTracker (maps the FlowTracker status to FocusLightClients - Status)
        /// </summary>
        /// <param name="newStatus"></param>
        private void setStatus(FocusState newStatus)
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

        /// <summary>
        /// starts a timer for the specified amount of minutes to enforce the status
        /// (doesn't change the status)
        /// </summary>
        /// <param name="minutes"></param>
        private void StartTimedEnforcing(int minutes)
        {
            Logger.WriteToConsole("FlowLight: Enforcing for " + minutes + ".");
            _enforcing = true;
            _enforcingTimer.Stop();
            _enforcingTimer.Interval = minutes * 60 * 1000;
            _enforcingTimer.Start();
        }

        /// <summary>
        /// starts enforcing the status infinitely
        /// (until the enforcing is cancelled)
        /// (doesn't change the status)
        /// </summary>
        private void StartInfiniteEnforcing()
        {
            Logger.WriteToConsole("FlowLight: Enforcing forever.");
            _enforcing = true;
        }

        /// <summary>
        /// stops the enforcing timer and sets enforcing to false
        /// (doesn't change the status)
        /// after this method has been called, FlowTracker can again change the state
        /// </summary>
        private void StopEnforcing()
        {
            Logger.WriteToConsole("FlowLight: Cancelling enforcing.");

            _enforcingTimer.Stop();
            _enforcing = false;
        }

        /// <summary>
        /// This method is executed when the enforcing timer has elapsed.
        /// It stops the enforcing (FlowTracker can change the status again)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnforcingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            StopEnforcing();
        }

        /// <summary>
        /// event arguments needed for the context menu of the application,
        /// for the menu items where the user can set the light to a certain state
        /// for e specified amount of minutes.
        /// </summary>
        public class MenuEventArgs : EventArgs
        {
            public Status Status { get; set; }
            public int Minutes { get; set; }

            public MenuEventArgs(Status status, int minutes)
            {
                Status = status;
                Minutes = minutes;
            }
        }

        /// <summary>
        /// This method handles the event if the user clicked on a context menu item to
        /// change the status for a certain amount of minutes.
        /// 
        /// This overrides any other state that is already set or any other enforcing timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void EnforcingClicked(object sender, MenuEventArgs e)
        {
            StartTimedEnforcing(e.Minutes);
            setStatus(Originator.User, e.Status);
        }

        

        /// <summary>
        /// This method handles if the computer is locked or unlocked and sets the status to
        /// Away if it has just been locked, and to Free if it has just been unlocked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        #endregion
    }
}
