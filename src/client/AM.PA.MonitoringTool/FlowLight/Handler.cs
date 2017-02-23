// Created by Manuela Zueger (zueger@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-18
// 
// Licensed under the MIT License.

using ABB.FocuslightApp.Clients;
using FlowTracker.Service;
using Microsoft.Win32;
using Shared;
using Shared.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Timers;
using System.Windows;

namespace FlowLight
{
    /// <summary>
    /// Class which manages the flowlight (and connection to Skype4Business)
    /// </summary>
    public class Handler
    {
        enum Originator { System, Skype, FlowTracker, User };
        public enum EnforceStatus { Free, Busy, DnD };

        public bool IsRunning;
        private static Handler _handler;
        private bool _flowLightEnabled;
        private bool _automaticEnabled;
        private bool _dndEnabled;
        private int _sensitivityLevel;
        private bool _skypeForBusinessEnabled;
        private Timer _updateTimer;
        private bool _locked;
        private bool _enforcing;
        private Timer _enforcingTimer;
        private Blink1Client _lightClient;
        private LyncStatus _skypeClient;
        private Status _currentFlowLightStatus;
        private List<ITracker> _trackers;

        #region Settings Properties

        public bool FlowLightEnabled
        {
            get
            {
                _flowLightEnabled = Database.GetInstance().GetSettingsBool("FlowLightEnabled", Settings.IsEnabledByDefault);
                return _flowLightEnabled;
            }
            set
            {
                var flowLightEnabled = value;

                // only update if settings changed
                if (flowLightEnabled == _flowLightEnabled) return;

                // update settings
                Database.GetInstance().SetSettings("FlowLightEnabled", value);

                // start/stop tracker if necessary
                if (!flowLightEnabled && IsRunning)
                {
                    Stop();
                }
                else if (flowLightEnabled && !IsRunning)
                {
                    Start(_trackers);
                }

                // log
                Database.GetInstance().LogInfo("The participant updated the setting 'FlowLightEnabled' to " + flowLightEnabled);
            }
        }

        public bool SkypeForBusinessEnabled
        {
            get
            {
                _skypeForBusinessEnabled = Database.GetInstance().GetSettingsBool("FlowLightSkypeForBusinessEnabled", Settings.IsSkypeForBusinessEnabledByDefault);
                return _skypeForBusinessEnabled;
            }
            set
            {
                var skypeForBusinessEnabled = value;

                // only update if settings changed
                if (skypeForBusinessEnabled == _skypeForBusinessEnabled) return;

                // update settings
                Database.GetInstance().SetSettings("FlowLightSkypeForBusinessEnabled", value);

                // log
                Database.GetInstance().LogInfo("The participant updated the setting 'FlowLightSkypeForBusinessEnabled' to " + skypeForBusinessEnabled);
            }
        }

        /// <summary>
        /// specifies whether the FlowTracker can automatically update the status based on the activity (= true) or if only manual
        /// changes are possible (= false)
        /// </summary>
        public bool AutomaticEnabled
        {
            get
            {
                _automaticEnabled = Database.GetInstance().GetSettingsBool("FlowLightAutomaticEnabled", Settings.IsAutomaticByDefault);
                return _automaticEnabled;
            }
            set
            {
                var automaticEnabled = value;

                // only update if settings changed
                if (automaticEnabled == _automaticEnabled) return;

                // update settings
                Database.GetInstance().SetSettings("FlowLightAutomaticEnabled", value);

                // log
                Database.GetInstance().LogInfo("The participant updated the setting 'FlowLightAutomaticEnabled' to " + automaticEnabled);
            }
        }

        /// <summary>
        /// specifies whether the FlowLight can automatically go to DnD (= true) or max. to Busy (= false)
        /// </summary>
        public bool DnDEnabled
        {
            get
            {
                _dndEnabled = Database.GetInstance().GetSettingsBool("FlowLightDnDEnabled", Settings.IsDnDAllowedByDefault);
                return _dndEnabled;
            }
            set
            {
                var dndEnabled = value;

                // only update if settings changed
                if (dndEnabled == _dndEnabled) return;

                // update settings
                Database.GetInstance().SetSettings("FlowLightDnDEnabled", value);

                // log
                Database.GetInstance().LogInfo("The participant updated the setting 'FlowLightDnDEnabled' to " + dndEnabled);
            }
        }

        /// <summary>
        /// the sensitivity of the FlowTracker, can be between 0 and 4 (0 = low, 2 = medium, 4 = high)
        /// </summary>
        public int SensitivityLevel
        {
            get
            {
                _sensitivityLevel = Database.GetInstance().GetSettingsInt("FlowLightSensitivityLevel", Settings.DefaultSensitivityLevel);
                return _sensitivityLevel;
            }
            set
            {
                var sensitivityLevel = value;

                // only update if settings changed
                if (sensitivityLevel == _sensitivityLevel) return;

                // update settings
                Database.GetInstance().SetSettings("FlowLightSensitivityLevel", value.ToString());

                // update FlowTracker
                UpdateSensitivityInFlowTracker();

                // log
                Database.GetInstance().LogInfo("The participant updated the setting 'FlowLightSensitivityLevel' to " + sensitivityLevel);
            }
        }

        

        #endregion

        #region Start/Stop & Initialization of Singleton

        public static Handler GetInstance()
        {
            return _handler ?? (_handler = new Handler());
        }

        public Handler()
        {
            _lightClient = new Blink1Client();
            _skypeClient = new LyncStatus();
        }

        public void Start(List<ITracker> trackers)
        {
            _trackers = trackers;

            // on first start, a pop-up is shown to ask the user to enable/disable the FlowLight
            if (IsFlowLightsFirstUse())
            {
                var msg = string.Format(CultureInfo.InvariantCulture, "FlowLight is a traffic-light like LED that indicates your availability for interruptions to your co-workers. It is also synched with your Skype for Business status. You can manually disable or enable the FlowLight anytime in the settings.\n\nDo you want to enable the FlowLight?");
                var res = MessageBox.Show(msg, "FlowLight Setup",
                    MessageBoxButton.YesNo);
                if (res == MessageBoxResult.Yes)
                {
                    _flowLightEnabled = true;
                    Database.GetInstance().SetSettings("FlowLightTrackerEnabled", true);
                }
                else
                {
                    IsRunning = false;
                    FlowLightEnabled = false;
                    FlowTracker.Daemon flowTracker = GetFlowTracker();
                    if (flowTracker != null) flowTracker.Stop();
                    return; // don't start the FlowLight!
                }
            }

            UpdateSensitivityInFlowTracker();

            // register and start update timer (for FlowTracker)
            if (_updateTimer != null)
                Stop();
            _updateTimer = new Timer();
            _updateTimer.Interval = Settings.UpdateInterval * 1000;
            _updateTimer.Elapsed += UpdateTimer_Elapsed;
            _updateTimer.Start();

            // register event handler for status changes in skype
            _skypeClient.OnOutsideChange += SkypeClient_OnOutsideChange;

            // register enforcing timer (for manual state changes)
            _enforcingTimer = new Timer();
            _enforcingTimer.Elapsed += EnforcingTimer_Elapsed;

            // register event to track when work station is locked / unlocked
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;

            IsRunning = true;
        }

        public void Stop()
        {
            // stop update timer
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer.Dispose();
                _updateTimer = null;
            }

            // stop enforcing timer
            if (_enforcingTimer != null)
            {
                _enforcingTimer.Stop();
                _enforcingTimer.Dispose();
                _enforcingTimer = null;
            }

            // unregister event handler for status changes in Skype
            _skypeClient.OnOutsideChange -= SkypeClient_OnOutsideChange;

            // turn off the light
            _lightClient.Solid(0, 0, 0, 0);

            // unregister event to track when work station is locked / unlocked
            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;

            //also stop flowTracker
            var flowTracker = GetFlowTracker();
            if (flowTracker != null) flowTracker.Stop();

            IsRunning = false;
        }

        #endregion

        #region FlowLight

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
                SetStatus(Originator.System, Status.Away);
                _locked = true;
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                SetStatus(Originator.System, Status.Free);
                _locked = false;
            }
        }

        /// <summary>
        /// is run when the update timer elapsed and sets the status according to 
        /// Flowtracker, unless the computer is locked, or we are enforcing a certain state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Don't do anything if the work station is locked, we are enforcing a state, or it is set to manual changes only
            if (!_locked && !_enforcing && AutomaticEnabled)
            {
                // set the status to the one determined by FlowTracker
                FocusState newFlowStatus = FlowTracker.Data.Queries.GetCurrentSmoothedFocusState();
   
                // if DnD is not enabled, the status can max. go to busy
                if (newFlowStatus == FocusState.VeryHigh && !DnDEnabled)
                {
                    newFlowStatus = FocusState.High;
                }

                SetStatus(newFlowStatus);        

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
            // ignore all changes from Skype when it is disabled or if the change is originated by us.
            // Also ignore all changes from Skype if the workstation is locked
            // Reason: Sometimes Skype cannot reflect the correct status (Away) when the computer is locked.
            if (SkypeForBusinessEnabled && e.Outside && !_locked)
            {
                Logger.WriteToConsole("FlowLight: Skype status change detected: " + e.CurrentStatus + ", outside: " + e.Outside);

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

                SetStatus(Originator.Skype, e.CurrentStatus);
            }
        }

        /// <summary>
        /// sets the status of the light and skype
        /// </summary>
        /// <param name="originator"></param>
        /// <param name="newStatus"></param>
        private void SetStatus(Originator originator, Status newStatus)
        {
            //only do status changes if the new status is different from the old one
            if (_currentFlowLightStatus != newStatus)
            {
                // Skype should only be updated if it is origniated by someone else than Skype, otherwise it is already updated
                if (originator != Originator.Skype && SkypeForBusinessEnabled)
                {
                    _skypeClient.Status = newStatus;
                }

                _currentFlowLightStatus = newStatus;
                _lightClient.Status = newStatus;

                Logger.WriteToConsole("FlowLight: The status was set by " + originator + " to " + newStatus + ".");
                Database.GetInstance().LogInfo("FlowLight: The status was set by " + originator + " to " + newStatus + ".");
            }
        }

        /// <summary>
        /// sets the status from FlowTracker (maps the FlowTracker status to FocusLightClients - Status)
        /// </summary>
        /// <param name="newStatus"></param>
        private void SetStatus(FocusState newStatus)
        {
            switch (newStatus)
            {
                case FocusState.Low:
                case FocusState.Medium:
                    SetStatus(Originator.FlowTracker, Status.Free);
                    break;
                case FocusState.High:
                    SetStatus(Originator.FlowTracker, Status.Busy);
                    break;
                case FocusState.VeryHigh:
                    SetStatus(Originator.FlowTracker, Status.DoNotDisturb);
                    break;
            }
        }

        #region Enforcing state for a certain time or infinitely

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
        /// This method handles the event if the user clicked on a context menu item to
        /// change the status for a certain amount of minutes.
        /// 
        /// This overrides any other state that is already set or any other enforcing timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void EnforcingClicked(EnforceStatus status, int minutes)
        {
            
            StartTimedEnforcing(minutes);
            SetStatus(Originator.User, ParseEnforceStatus(status));
        }

        private Status ParseEnforceStatus(EnforceStatus enforceStatus)
        {
            switch (enforceStatus)
            {
                case EnforceStatus.Free:
                    return Status.Free;
                case EnforceStatus.Busy:
                    return Status.Busy;
                case EnforceStatus.DnD:
                    return Status.DoNotDisturb;
                default:
                    return Status.Free;
            }
        }

        /// <summary>
        /// this is executed when the user resets the enforcing via the context menu.
        /// this will stop the enforcing timer and set the status to free.
        /// </summary>
        public void ResetEnforcingClicked()
        {
            StopEnforcing();
            SetStatus(Originator.User, Status.Free);
        }

        #endregion

        #endregion

        #region Helpers

        private FlowTracker.Daemon GetFlowTracker()
        {
            try
            {
                var flowTracker = _trackers.Where(t => t.GetType() == typeof(FlowTracker.Daemon))
                            .Cast<FlowTracker.Daemon>()
                            .FirstOrDefault();

                if (flowTracker != null)
                {
                    return flowTracker;
                }

                return null;
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return null;
            }
        }

        private void UpdateSensitivityInFlowTracker()
        {
            FlowTracker.Daemon flowTracker = GetFlowTracker();
            if (flowTracker != null)
            {
                switch (SensitivityLevel)
                {
                    case 0:
                        flowTracker.SetSetting_Threshold_High_Percentile(0.95);
                        flowTracker.SetSetting_Threshold_VeryHigh_Percentile(0.98);
                        break;
                    case 1:
                        flowTracker.SetSetting_Threshold_High_Percentile(0.93);
                        flowTracker.SetSetting_Threshold_VeryHigh_Percentile(0.97);
                        break;
                    case 2:
                        flowTracker.SetSetting_Threshold_High_Percentile(0.91);
                        flowTracker.SetSetting_Threshold_VeryHigh_Percentile(0.96);
                        break;
                    case 3:
                        flowTracker.SetSetting_Threshold_High_Percentile(0.89);
                        flowTracker.SetSetting_Threshold_VeryHigh_Percentile(0.95);
                        break;
                    case 4:
                        flowTracker.SetSetting_Threshold_High_Percentile(0.87);
                        flowTracker.SetSetting_Threshold_VeryHigh_Percentile(0.94);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Checks if the office API is used for the first time
        /// </summary>
        /// <returns>true if there is no setting stored, else otherwise</returns>
        private bool IsFlowLightsFirstUse()
        {
            return !Database.GetInstance().HasSetting("FlowLightTrackerEnabled");
        }

        #endregion
    }
}
