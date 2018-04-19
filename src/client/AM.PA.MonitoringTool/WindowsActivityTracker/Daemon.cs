// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-30
// 
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using WindowsActivityTracker.Helpers;
using Shared;
using Shared.Data;
using System.Collections.Generic;
using WindowsActivityTracker.Visualizations;
using WindowsActivityTracker.Data;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Reflection;
using System.Linq;
using WindowsActivityTracker.Models;

namespace WindowsActivityTracker
{
    /// <summary>
    /// This tracker stores all program switches (window switch) and changes of the
    /// window titles in the database (using Windows Hooks and its events).
    /// </summary>
    public sealed class Daemon : BaseTrackerDisposable, ITracker
    {
        private bool _disposed = false;
        NativeMethods.WinEventDelegate _dele; // to ensure it's not collected while using
        private IntPtr _hWinEventHookForWindowSwitch;
        private IntPtr _hWinEventHookForWindowTitleChange;
        private Timer _idleCheckTimer;
        private Timer _idleSleepValidator;
        private NativeMethods.LASTINPUTINFO _lastInputInfo;
        private WindowsActivityEntry _previousEntry;
        private DateTime _previousIdleSleepValidated = DateTime.MinValue;

        #region ITracker Stuff

        public Daemon()
        {
            Name = "Windows Activity Tracker";
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _idleCheckTimer.Dispose();
                    _idleSleepValidator.Dispose();
                }

                // Release unmanaged resources.
                // Set large fields to null.                
                _disposed = true;
            }

            // Call Dispose on your base class.
            base.Dispose(disposing);
        }

        public override void Start()
        {
            try
            {
                // Register for Window Events
                _dele = new NativeMethods.WinEventDelegate(WinEventProc);
                _hWinEventHookForWindowSwitch = NativeMethods.SetWinEventHook(NativeMethods.EVENT_SYSTEM_FOREGROUND, NativeMethods.EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _dele, 0, 0, NativeMethods.WINEVENT_OUTOFCONTEXT);
                _hWinEventHookForWindowTitleChange = NativeMethods.SetWinEventHook(NativeMethods.EVENT_OBJECT_NAMECHANGE, NativeMethods.EVENT_OBJECT_NAMECHANGE, IntPtr.Zero, _dele, 0, 0, NativeMethods.WINEVENT_OUTOFCONTEXT);

                // Register for logout/shutdown event
                SystemEvents.SessionEnding += SessionEnding;
                SystemEvents.PowerModeChanged += OnPowerChange;

                // Register to check if idle or not
                if (Settings.RecordIdle)
                {
                    // reset everything properly
                    if (_idleCheckTimer != null || _idleSleepValidator != null) Stop();

                    // register for events
                    _idleCheckTimer = new Timer();
                    _idleCheckTimer.Interval = Settings.IdleTimerInterval_ms;
                    _idleCheckTimer.Elapsed += CheckIfIdleTime;
                    _idleCheckTimer.Start();

                    _idleSleepValidator = new Timer();
                    _idleSleepValidator.Interval = Settings.IdleSleepValidate_TimerInterval_ms;
                    _idleSleepValidator.Elapsed += ValidateSleepIdleTime;
                    _idleSleepValidator.Start();

                    _lastInputInfo = new NativeMethods.LASTINPUTINFO();
                    _lastInputInfo.cbSize = (uint)Marshal.SizeOf(_lastInputInfo);
                    _lastInputInfo.dwTime = 0;
                }

                IsRunning = true;
            }
            catch (Exception e)
            {
                Database.GetInstance().LogWarning("Registering events failed: " + e.Message);

                IsRunning = false;
            }
        }

        public override void Stop()
        {
            try
            {
                // insert idle event (as last entry
                SetCurrentAndStoreThisAndPrevious_WindowsActivityEvent("Tracker stopped", Dict.Idle);

                // Unregister for window events
                NativeMethods.UnhookWinEvent(_hWinEventHookForWindowSwitch);
                NativeMethods.UnhookWinEvent(_hWinEventHookForWindowTitleChange);

                // Unregister for logout/shutdown event
                SystemEvents.SessionEnding -= SessionEnding;

                // Unregister idle time checker Timer
                if (_idleCheckTimer != null)
                {
                    _idleCheckTimer.Stop();
                    _idleCheckTimer.Dispose();
                    _idleCheckTimer = null;
                }

                // Unregister idle resume validator Timer
                if (_idleSleepValidator != null)
                {
                    _idleSleepValidator.Stop();
                    _idleSleepValidator.Dispose();
                    _idleSleepValidator = null;
                }
            }
            catch (Exception e)
            {
                Database.GetInstance().LogWarning("Un-Registering events failed: " + e.Message);
            }

            IsRunning = false;
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            Queries.CreateWindowsActivityTable();
        }

        public override void UpdateDatabaseTables(int version)
        {
            Queries.UpdateDatabaseTables(version);
        }

        public override bool IsEnabled()
        {
            return Settings.IsEnabled;
        }

        public override string GetVersion()
        {
            var v = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;
            return Shared.Helpers.VersionHelper.GetFormattedVersion(v);
        }

        public override List<IVisualization> GetVisualizationsDay(DateTimeOffset date)
        {
            var vis1 = new DayProgramsUsedPieChart(date);
            var vis2 = new DayMostFocusedProgram(date);
            var vis3 = new DayFragmentationTimeline(date);
            return new List<IVisualization> { vis1, vis2, vis3 };
        }

        public override List<IVisualization> GetVisualizationsWeek(DateTimeOffset date)
        {
            var vis1 = new WeekProgramsUsedTable(date);
            var vis2 = new WeekWorkTimeBarChart(date);
            return new List<IVisualization> { vis1, vis2 };
        }

        #endregion

        #region Set Previous Entry and store it

        /// <summary>
        /// Saves the previouis WindowsActivityEvent into the database
        /// (also re-sets the previous item values)
        /// </summary>
        /// <param name="windowTitle"></param>
        /// <param name="process"></param>
        private void SetCurrentAndStorePrevious_WindowsActivityEvent(DateTime tsStart, string windowTitle, string process)
        {
            // previous entry is set in following method
            SetCurrentAndStorePrevious_WindowsActivityEvent(tsStart, windowTitle, process, IntPtr.Zero);
        }

        /// <summary>
        /// Saves the previous WindowsActivityEvent into the database 
        /// 
        /// (tsEnd not know yet for current entry)
        /// (includes a process handle for later)
        /// (also re-sets the previous item values)
        /// </summary>
        /// <param name="windowTitle"></param>
        /// <param name="process"></param>
        private void SetCurrentAndStorePrevious_WindowsActivityEvent(DateTime tsStart, string windowTitle, string process, IntPtr handle)
        {
            var tmpPreviousEntry = _previousEntry; // perf, because storing takes a moment

            // set current entry
            _previousEntry = new WindowsActivityEntry(tsStart, windowTitle, process, handle); // tsEnd is not known yet

            // update tsEnd & store previous entry
            if (tmpPreviousEntry != null)
            {
                tmpPreviousEntry.TsEnd = DateTime.Now;
                Queries.InsertSnapshot(tmpPreviousEntry);
            }
        }

        /// <summary>
        /// Saves the previous and current WindowsActivityEvent into the database
        /// (also re-sets the previous item values)
        /// 
        /// !! Only used when tsEnd will not be available later, as the tool/computer is being shut down
        /// </summary>
        /// <param name="tsStart"></param>
        /// <param name="tsEnd"></param>
        /// <param name="windowTitle"></param>
        /// <param name="process"></param>
        private void SetCurrentAndStoreThisAndPrevious_WindowsActivityEvent(string windowTitle, string process)
        {
            // store previous entry
            //var tmpPreviousEntry = _previousEntry; // perf, because storing takes a moment

            if (_previousEntry != null)
            {
                SetCurrentAndStorePrevious_WindowsActivityEvent(_previousEntry.TsStart, _previousEntry.WindowTitle, _previousEntry.Process, _previousEntry.Handle);
            }

            // set current entry
            _previousEntry = new WindowsActivityEntry(DateTime.Now, DateTime.Now, windowTitle, process, IntPtr.Zero); // tsEnd is right now

            // set store current entry
            Queries.InsertSnapshot(_previousEntry);
        }

        #endregion

        #region Idle Time Checker

        /// <summary>
        /// Checks if the last input timestamp was before the idle-interval
        /// </summary>
        /// <returns></returns>
        private bool WasIdleInLastInterval()
        {
            // get a timestamp of the last user input
            NativeMethods.GetLastInputInfo(ref _lastInputInfo);

            // idle if no input for more than 'Interval' milliseconds (120s) 
            // now_ts - lastinput_ts > 120s => IDLE
            var isIdle = ((Environment.TickCount - _lastInputInfo.dwTime) > Settings.NotCountingAsIdleInterval_ms);

            return isIdle;
        }

        /// <summary>
        ///  check every 10 seconds if the user has been idle for the past 120s
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckIfIdleTime(object sender, ElapsedEventArgs e)
        {
            if (_previousEntry == null) return; // no need to check if not available

            var isIdle = WasIdleInLastInterval();

            if (isIdle && _previousEntry.WasIdle)
            {
                // don't save, already saved
            }
            else if (isIdle && !_previousEntry.WasIdle)
            {
                // store Idle (i.e. from process -> IDLE)
                SetCurrentAndStorePrevious_WindowsActivityEvent(
                    DateTime.Now.AddMilliseconds(-Settings.NotCountingAsIdleInterval_ms), // subtract IDLE time
                    Dict.Idle, Dict.Idle, IntPtr.Zero);
            }
            else if (! isIdle && _previousEntry.WasIdle)
            {
                // resumed work in the same program (i.e. from IDLE -> current process)
                StoreProcess();
                // TODO later: maybe check if not just moved the mouse a little, but actually inserted some data
            }
            else if (! isIdle && !_previousEntry.WasIdle)
            {
                // nothing to do here
            }
        }

        #endregion

        #region Validate (+ Fix) Idle Sleep (sleep bug that sometimes doesn't catch sleep events)

        private void ValidateSleepIdleTime(object sender, ElapsedEventArgs e)
        {
            try
            {
                // if user input table is not available => stop timer
                if (!Queries.UserInputTableExists())
                {
                    _idleSleepValidator.Stop();
                    _idleSleepValidator.Dispose();
                    _idleSleepValidator = null;
                    return;
                }

                // get list of all IDLE errors within time frame
                var toFix = PrepareIntervalAndGetMissedSleepEvents();

                // add IDLE entry NotCountingAsIdleInterval_ms after entry
                Queries.AddMissedSleepIdleEntry(toFix);

                // reset (to not recheck everything every time)
                _previousIdleSleepValidated = DateTime.Now;
                Database.GetInstance().SetSettings(Settings.IdleSleepLastValidated, _previousIdleSleepValidated.ToString(DatabaseImplementation.DB_FORMAT_DAY_AND_TIME));
            }
            catch (Exception ex) 
            {
                Database.GetInstance().LogError(ex.Message);
            }
        }

        private List<Tuple<long, DateTime, DateTime>> PrepareIntervalAndGetMissedSleepEvents()
        {
            // check if the validation should go back long
            var isLongCheck = true;
            if (_previousIdleSleepValidated == DateTime.MinValue)
            {
                var db_previousIdleSleepValidated = Database.GetInstance().GetSettingsDate(Settings.IdleSleepLastValidated, DateTimeOffset.MinValue);

                if (db_previousIdleSleepValidated.Date == DateTimeOffset.Now.Date)
                {
                    isLongCheck = false;
                    _previousIdleSleepValidated = DateTime.Now;
                }
            }
            else
            {
                if (_previousIdleSleepValidated.Date == DateTime.Now.Date)
                {
                    isLongCheck = false;
                }
            }

            // set timespan where idle is validated (short or long)
            DateTime ts_checkFrom = (isLongCheck)
                                    ? DateTime.Now.AddDays(-Settings.IdleSleepValidate_ThresholdBack_long_d) // go a couple of days back to check
                                    : _previousIdleSleepValidated.AddHours(-Settings.IdleSleepValidate_ThresholdBack_short_h); // check from previously checked datetime (and increase the interval a little)
            DateTime ts_checkTo = DateTime.Now;

            // reset _previousIdleSleepValidated in method that calls this one
            return Queries.GetMissedSleepEvents(ts_checkFrom, ts_checkTo);
        }

        #endregion

        #region SetWinEventHooks

        /// <summary>
        /// Catch Window Switch Events
        /// </summary>
        /// <param name="hWinEventHook"></param>
        /// <param name="eventType"></param>
        /// <param name="hwnd"></param>
        /// <param name="idObject"></param>
        /// <param name="idChild"></param>
        /// <param name="dwEventThread"></param>
        /// <param name="dwmsEventTime"></param>
        public async void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            try
            {
                // filter out non-HWND namechanges... (eg. items within a listbox)
                if (idObject != 0 || idChild != 0)
                {
                    return;
                }

                // run on separate thread (to avoid lags)
                await Task.Run(() => StoreProcess());
            }
            catch (Exception e)
            {
                Database.GetInstance().LogWarning("Failed to : " + e.Message);
            }
        }

        private void StoreProcess()
        {
            // get start time stamp
            var currentTimeStamp = DateTime.Now;

            // get current window title
            var currentHandle = IntPtr.Zero;
            currentHandle = NativeMethods.GetForegroundWindow();
            var currentWindowTitle = GetActiveWindowTitle(currentHandle);

            // get current process name
            var currentProcess = GetProcessName(currentHandle);

            // [special case] lockscreen (shutdown and logout events are handled separately)
            if (!string.IsNullOrEmpty(currentProcess) && currentProcess.Trim().ToLower(CultureInfo.InvariantCulture).Contains("lockapp"))
            {
                currentWindowTitle = "LockScreen";
                currentProcess = Dict.Idle;

                // as the logout/shutdown-event is sometimes missed, we try to fix this when the user resumes
                //ResumeComputer_IdleChecker();
            }
            // [special case] slidetoshutdown (shutdown and logout events are handled separately)
            else if (!string.IsNullOrEmpty(currentProcess) && currentProcess.Trim().ToLower(CultureInfo.InvariantCulture).Contains("slidetoshutdown"))
            {
                currentWindowTitle = "SlideToShutDown";
                currentProcess = Dict.Idle;
            }
            // [special case] Windows 10 apps (e.g. Edge, Photos, Mail)
            else if (!string.IsNullOrEmpty(currentProcess) && currentProcess.ToLower().Equals("applicationframehost"))
            {
                var lastDash = currentWindowTitle.LastIndexOf("- ");
                if (lastDash > 0)
                {
                    var processName = currentWindowTitle.Substring(lastDash).Replace("- ", "").Trim();
                    if (!string.IsNullOrEmpty(processName)) currentProcess = processName;
                }
                else
                {
                    currentProcess = currentWindowTitle;
                }
            }
            // add more special cases here if necessary

            // save if process or window title changed and user was not IDLE in past interval
            var differentProcessNotIdle = !string.IsNullOrEmpty(currentProcess) && (_previousEntry == null || _previousEntry.Process != currentProcess) && currentProcess.Trim().ToLower(CultureInfo.InvariantCulture) != Dict.Idle.ToLower(CultureInfo.InvariantCulture);
            var differentWindowTitle = !string.IsNullOrEmpty(currentWindowTitle) && (_previousEntry == null || _previousEntry.WindowTitle != currentWindowTitle);
            var notIdleLastInterval = !WasIdleInLastInterval();

            // is a WindowActivityEntry-Switch
            if ((differentProcessNotIdle || differentWindowTitle) && notIdleLastInterval)
            {
                SetCurrentAndStorePrevious_WindowsActivityEvent(currentTimeStamp, currentWindowTitle, currentProcess, currentHandle);
            }
        }

        /// <summary>
        /// Catch logout and shutdown (also restart) event
        /// (Hint: this event for some reason is not always catched/thrown...)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SessionEnding(object sender, SessionEndingEventArgs e)
        {
            if (e.Reason == SessionEndReasons.Logoff)
            {
                SetCurrentAndStoreThisAndPrevious_WindowsActivityEvent("Logoff", Dict.Idle);
            }
            else if (e.Reason == SessionEndReasons.SystemShutdown)
            {
                SetCurrentAndStoreThisAndPrevious_WindowsActivityEvent("SystemShutdown", Dict.Idle);
            }
        }

        /// <summary>
        /// Catch PowerMode-changes (e.g. resume computer, suspend computer, change charging)
        /// (Hint: this event for some reason is not always catched/thrown...)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPowerChange(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                //ResumeComputer_IdleChecker();
            }
            else if (e.Mode == PowerModes.Suspend)
            {
                SetCurrentAndStorePrevious_WindowsActivityEvent(DateTime.Now, "Suspend", Dict.Idle);
            }
            else if (e.Mode == PowerModes.StatusChange)
            {
                // handle docking station stuff here?
            }
        }

        /// <summary>
        /// Get the name of the current process
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        private string GetProcessName(IntPtr handle)
        {
            try
            {
                // performance: if same handle than previously, just return the process-name
                if (_previousEntry != null && _previousEntry.Handle == handle) return _previousEntry.Process;

                // else: get the process name from the list of processes
                uint processId;
                NativeMethods.GetWindowThreadProcessId(handle, out processId);
                var processlist = Process.GetProcesses();
                return processlist.FirstOrDefault(pr => pr.Id == processId).ProcessName;

                // 2017-01-24 usually slower:
                //var pN = Process.GetProcessById((int)processId).ProcessName;
            }
            catch {}
            return string.Empty;
        }

        /// <summary>
        /// Get the name of the currently active window
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        private static string GetActiveWindowTitle(IntPtr handle)
        {
            try
            {
                const int nChars = 256;
                var buff = new StringBuilder(nChars);

                if (handle != IntPtr.Zero && NativeMethods.GetWindowText(handle, buff, nChars) > 0)
                {
                    return buff.ToString();
                }
            }
            catch {}
            return string.Empty;
        }

        #endregion
    }
}
