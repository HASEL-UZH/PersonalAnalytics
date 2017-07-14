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
        private PreviousWindowsActivityEntry _previousEntry = new PreviousWindowsActivityEntry();

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
                SetAndStoreProcessAndWindowTitle("Tracker stopped", Dict.Idle);

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
            // no database updates necessary yet
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
            var vis = new WeekProgramsUsedTable(date);
            return new List<IVisualization> { vis };
        }

        #endregion

        #region Set Previous Entry and store it

        /// <summary>
        /// Saves the Windows Activity Event into the database
        /// (also re-sets the previous item values)
        /// </summary>
        /// <param name="windowTitle"></param>
        /// <param name="process"></param>
        private void SetAndStoreProcessAndWindowTitle(string windowTitle, string process)
        {
            SetAndStoreProcessAndWindowTitle(windowTitle, process, IntPtr.Zero);
        }

        /// <summary>
        /// Saves the Windows Activity Event into the database (including a process handle)
        /// (also re-sets the previous item values)
        /// </summary>
        /// <param name="windowTitle"></param>
        /// <param name="process"></param>
        private void SetAndStoreProcessAndWindowTitle(string windowTitle, string process, IntPtr handle)
        {
            _previousEntry = new PreviousWindowsActivityEntry(DateTime.Now, windowTitle, process, handle);
            Queries.InsertSnapshot(windowTitle, process);
        }

        /// <summary>
        /// Saves the Windows Activity Event into the database (including a process handle and manual timestamp, used e.g. for IDLE time -2min)
        /// (also re-sets the previous item values)
        /// </summary>
        /// <param name="windowTitle"></param>
        /// <param name="process"></param>
        private void SetAndStoreProcessAndWindowTitle(string windowTitle, string process, IntPtr handle, DateTime manualTimeStamp)
        {
            _previousEntry = new PreviousWindowsActivityEntry(manualTimeStamp, windowTitle, process, handle);
            Queries.InsertSnapshot(windowTitle, process, manualTimeStamp);
        }

        private void StoreProcessAndWindowTitle(string windowTitle, string process, DateTime manualTimeStamp)
        {
            // do not override previous entry (as this is a hack/fix for a missed IDLE entry)
            Queries.InsertSnapshot(windowTitle, process, manualTimeStamp);
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
            var isIdle = WasIdleInLastInterval();

            if (isIdle && _previousEntry.WasIdle)
            {
                // don't save, already saved
            }
            else if (isIdle && !_previousEntry.WasIdle)
            {
                // store Idle (i.e. from process -> IDLE; also subtract IDLE time)
                SetAndStoreProcessAndWindowTitle(Dict.Idle, Dict.Idle, IntPtr.Zero, DateTime.Now.AddMilliseconds(- Settings.NotCountingAsIdleInterval_ms));
            }
            else if (! isIdle && _previousEntry.WasIdle)
            {
                // resumed work in the same program (i.e. from IDLE -> current process)
                StoreProcess();
                //TODO: maybe check if not just moved the mouse a little, but actually inserted some data
            }
            else if (! isIdle && !_previousEntry.WasIdle)
            {
                // nothing to do here
            }
        }

        #endregion

        #region Idle Sleep Checker (sleep bug that sometimes doesn't catch sleep events)

        private DateTime _previousIdleSleepValidated = DateTime.MinValue;

        private void ValidateSleepIdleTime(object sender, ElapsedEventArgs e)
        {
            // if user input table is not available => stop timer
            if (! Queries.UserInputTableExists())
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
        }

        private List<DateTime> PrepareIntervalAndGetMissedSleepEvents()
        {
            DateTime ts_checkFrom = (_previousIdleSleepValidated.Date == DateTime.Now.Date)
                                    ? _previousIdleSleepValidated.AddHours(-2) // check from previously checked datetime (and increase the interval a little)
                                    : DateTime.Now.AddDays(-Settings.IdleSleepValidate_ThresholdBack_d); // go a couple of days back to check
            DateTime ts_checkTo = DateTime.Now;

            _previousIdleSleepValidated = DateTime.Now; // reset (to not recheck everything every time)
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
            var differentProcessNotIdle = !string.IsNullOrEmpty(currentProcess) && _previousEntry.Process != currentProcess && currentProcess.Trim().ToLower(CultureInfo.InvariantCulture) != Dict.Idle.ToLower(CultureInfo.InvariantCulture);
            var differentWindowTitle = !string.IsNullOrEmpty(currentWindowTitle) && _previousEntry.WindowTitle != currentWindowTitle;
            var notIdleLastInterval = !WasIdleInLastInterval();

            if ((differentProcessNotIdle || differentWindowTitle) && notIdleLastInterval)
            {
                SetAndStoreProcessAndWindowTitle(currentWindowTitle, currentProcess, currentHandle);
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
                SetAndStoreProcessAndWindowTitle("Logoff", Dict.Idle);
            }
            else if (e.Reason == SessionEndReasons.SystemShutdown)
            {
                SetAndStoreProcessAndWindowTitle("SystemShutdown", Dict.Idle);
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
                SetAndStoreProcessAndWindowTitle("Suspend", Dict.Idle);
            }
            else if (e.Mode == PowerModes.StatusChange)
            {
                // todo: handle docking station stuff here?
            }
        }

        /// <summary>
        /// This method is called in case the user resumes the computer.
        /// As the sleep/logout-events are not always catched, we have to check
        /// if they were catched the last time, and if not fix it.
        /// 
        /// (it's also called when the user goes to the lockscreen, but not executed,
        /// as the WasIdleInLastInterval is false)
        /// </summary>
        //private void ResumeComputer_IdleChecker()
        //{
        //    if (_previousEntry.Process != Dict.Idle && WasIdleInLastInterval())
        //    {
        //        // TODO: catch timestamp of last entry here (+ go forward until 2+ mins with no user input)
        //        var manualTimeStamp = _previousEntry.TimeStamp.AddMilliseconds(- Settings.NotCountingAsIdleInterval);
        //        StoreProcessAndWindowTitle("ManualSleep", Dict.Idle, manualTimeStamp);

        //        // TODO: remove logger (only for testing)
        //        Logger.WriteToLogFile(new Exception("Fixed? ManualSleep (previous: " + _previousEntry.TimeStamp + " p: " + _previousEntry.Process + " w: " + _previousEntry.WindowTitle));
        //    }
        //}

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
                if (_previousEntry.Handle == handle) return _previousEntry.Process;

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

    /// <summary>
    /// Helper class to handle the previous entry values
    /// </summary>
    internal class PreviousWindowsActivityEntry
    {
        public PreviousWindowsActivityEntry()
        {
            TimeStamp = DateTime.MinValue;
            Handle = IntPtr.Zero;
        }

        public PreviousWindowsActivityEntry(DateTime timeStamp, string windowTitle, string process, IntPtr handle)
        {
            TimeStamp = timeStamp;
            WindowTitle = windowTitle;
            Process = process;
            Handle = handle;
        }

        public DateTime TimeStamp { get; set; }
        public string WindowTitle { get; set; }
        public string Process { get; set; }
        public IntPtr Handle { get; set; }
        public bool WasIdle { get { return (Process == Dict.Idle); } }
    }
}
