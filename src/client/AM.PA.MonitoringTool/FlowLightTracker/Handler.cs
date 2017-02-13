using ABB.FocuslightApp.Clients;
using FocusLightTracker.Service;
using Microsoft.Win32;
using Shared;
using Shared.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace FlowLight
{
    public class Handler
    {
        enum Originator {System, Skype, FlowTracker, User };

        public bool IsRunning;
        private static Handler _handler;
        private bool _flowLightEnabled;
        private Timer _updateTimer;
        private bool _locked;
        private bool _enforcing;
        private Timer _enforcingTimer;
        private Blink1Client _lightClient;
        private LyncStatus _skypeClient;
        private Status _currentFlowLightStatus;
        private List<ITracker> _trackers;

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

        public bool FlowLightEnabled
        {
            get
            {
                _flowLightEnabled = Database.GetInstance().GetSettingsBool("FlowLightTrackerEnabled", Settings.IsEnabledByDefault);
                return _flowLightEnabled;
            }
            set
            {
                var updatedIsEnabled = value;

                // only update if settings changed
                if (updatedIsEnabled == _flowLightEnabled) return;

                // update settings
                Database.GetInstance().SetSettings("FlowLightTrackerEnabled", value);

                // start/stop tracker if necessary
                if (!updatedIsEnabled && IsRunning)
                {
                    Stop();
                }
                else if (updatedIsEnabled && !IsRunning)
                {
                    Start(_trackers);
                }

                // log
                Database.GetInstance().LogInfo("The participant updated the setting 'FlowLightTrackerEnabled' to " + updatedIsEnabled);
            }
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
                    StopFlowTracker();
                    return; // don't start the FlowLight!
                }
            }

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

        /// <summary>
        /// Checks if the office API is used for the first time
        /// </summary>
        /// <returns>true if there is no setting stored, else otherwise</returns>
        private bool IsFlowLightsFirstUse()
        {
            return !Database.GetInstance().HasSetting("FlowLightTrackerEnabled");
        }

        public void Stop()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer.Dispose();
                _updateTimer = null;
            }

            _skypeClient.OnOutsideChange -= SkypeClient_OnOutsideChange;

            //also stop flowTracker
            StopFlowTracker();

            IsRunning = false;
        }

        private void StopFlowTracker()
        {
            try
            {
                var flowTracker = _trackers.Where(t => t.GetType() == typeof(FocusLightTracker.Daemon))
                            .Cast<FocusLightTracker.Daemon>()
                            .FirstOrDefault();

                if (flowTracker != null)
                {
                    flowTracker.Stop();
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
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
                setStatus(Originator.System, Status.Away);
                _locked = true;
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                setStatus(Originator.System, Status.Free);
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
            // Don't do anything if the work station is locked or we are enforcing a state
            if (!_locked && !_enforcing)
            {
                // set the status to the one determined by FlowTracker
                FocusState newFlowStatus = FocusLightTracker.Data.Queries.GetCurrentSmoothedFocusState();
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
            if (!_locked && e.Outside)
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
            //only do status changes if the new status is different from the old one
            if (_currentFlowLightStatus != newStatus)
            {
                // Skype should only be updated if it is origniated by someone else than Skype, otherwise it is already updated
                if (originator != Originator.Skype)
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
        public void EnforcingClicked(Status status, int minutes)
        {
            StartTimedEnforcing(minutes);
            setStatus(Originator.User, status);
        }

        /// <summary>
        /// this is executed when the user resets the enforcing via the context menu.
        /// this will stop the enforcing timer and set the status to free.
        /// </summary>
        public void ResetEnforcingClicked()
        {
            StopEnforcing();
            setStatus(Originator.User, Status.Free);
        }

        #endregion

        #endregion
    }
}
