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
        private string _previousWindowTitleEntry = string.Empty;
        private string _previousProcess = string.Empty;
        private IntPtr _previousHandle = IntPtr.Zero;
        private bool _lastEntryWasIdle = false;
        private NativeMethods.LASTINPUTINFO _lastInputInfo;

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
                    if (_idleCheckTimer != null)
                        Stop();
                    _idleCheckTimer = new Timer();
                    _idleCheckTimer.Interval = Settings.IdleTimerIntervalInMilliseconds;
                    _idleCheckTimer.Elapsed += CheckIfIdleTime;
                    _idleCheckTimer.Start();

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

                // Unregister idle time checker
                if (_idleCheckTimer != null)
                {
                    _idleCheckTimer.Stop();
                    _idleCheckTimer.Dispose();
                    _idleCheckTimer = null;
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

        /// <summary>
        /// Saves the Windows Activity Event into the database
        /// (also re-sets the previous item values)
        /// </summary>
        /// <param name="windowTitle"></param>
        /// <param name="process"></param>
        private void SetAndStoreProcessAndWindowTitle(string windowTitle, string process)
        {
            _previousWindowTitleEntry = windowTitle;
            _previousProcess = process;
            _lastEntryWasIdle = (process == Dict.Idle);

            Queries.InsertSnapshot(windowTitle, process);
        }

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
            var isIdle = ((Environment.TickCount - _lastInputInfo.dwTime) > Settings.NotCountingAsIdleInterval);

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

            if (isIdle && _lastEntryWasIdle)
            {
                // don't save, already saved
            }
            else if (isIdle && ! _lastEntryWasIdle)
            {
                // store Idle (i.e. from process -> IDLE)
                SetAndStoreProcessAndWindowTitle(Dict.Idle, Dict.Idle); 
            }
            else if (! isIdle && _lastEntryWasIdle)
            {
                // resumed work in the same program (i.e. from IDLE -> current process)
                StoreProcess();
                //TODO: maybe check if not just moved the mouse a little, but actually inserted some data
            }
            else if (! isIdle && ! _lastEntryWasIdle)
            {
                // nothing to do here
            }
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
            var handle = IntPtr.Zero;
            handle = NativeMethods.GetForegroundWindow();
            var currentWindowTitle = GetActiveWindowTitle(handle);

            // get current process name
            var currentProcess = GetProcessName(handle);

            // [special case] lockscreen (shutdown and logout events are handled separately)
            if (!string.IsNullOrEmpty(currentProcess) && currentProcess.Trim().ToLower(CultureInfo.InvariantCulture).Contains("lockapp"))
            {
                currentWindowTitle = "LockScreen";
                currentProcess = Dict.Idle;

                ResumeComputerIdleChecker();
            }
            // [special case] slidetoshutdown (shutdown and logout events are handled separately)
            else if (!string.IsNullOrEmpty(currentProcess) && currentProcess.Trim().ToLower(CultureInfo.InvariantCulture).Contains("slidetoshutdown"))
            {
                currentWindowTitle = "SlideToShutDown";
                currentProcess = Dict.Idle;
            }
            // [special case] Windows 10 apps (e.g. Edge, Photos, Mail)
            else if (currentProcess.ToLower().Equals("applicationframehost"))
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
            //add more special cases if necessary

            // save if process or window title changed and user was not IDLE in past interval
            var differentProcessNotIdle = !string.IsNullOrEmpty(currentProcess) && _previousProcess != currentProcess && currentProcess.Trim().ToLower(CultureInfo.InvariantCulture) != Dict.Idle.ToLower(CultureInfo.InvariantCulture);
            var differentWindowTitle = !string.IsNullOrEmpty(currentWindowTitle) && _previousWindowTitleEntry != currentWindowTitle;
            //var notIdleLastInterval = !WasIdleInLastInterval(); // TODO: why do we have this?

            if ((differentProcessNotIdle || differentWindowTitle)) // && notIdleLastInterval)
            {
                _previousHandle = handle;
                SetAndStoreProcessAndWindowTitle(currentWindowTitle, currentProcess);
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
                ResumeComputerIdleChecker();
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

        private void ResumeComputerIdleChecker()
        {
            // TODO: implement
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
                if (_previousHandle == handle) return _previousProcess;

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
            catch { }
            return string.Empty;
        }

        #endregion
    }
}
