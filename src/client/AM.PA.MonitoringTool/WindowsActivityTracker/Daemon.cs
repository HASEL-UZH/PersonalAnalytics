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
        private bool _lastEntryWasIdle = false;

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
                Queries.InsertSnapshot("Tracker stopped", Dict.Idle);

                // unregister for window events
                NativeMethods.UnhookWinEvent(_hWinEventHookForWindowSwitch);
                NativeMethods.UnhookWinEvent(_hWinEventHookForWindowTitleChange);

                // unregister idle time checker
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

        public override List<IVisualization> GetVisualizationsDay(DateTimeOffset date)
        {
            var vis1 = new DayProgramsUsedPieChart(date);
            var vis2 = new DayMostFocusedProgram(date);
            return new List<IVisualization> { vis1, vis2 };
        }

        public override List<IVisualization> GetVisualizationsWeek(DateTimeOffset date)
        {
            var vis = new WeekProgramsUsedTable(date);
            return new List<IVisualization> { vis };
        }

        #endregion

        #region Idle Time Checker

        private NativeMethods.LASTINPUTINFO _lastInputInfo;

        /// <summary>
        ///  check every 10 seconds if the user has been idle for the past 120s
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckIfIdleTime(object sender, ElapsedEventArgs e)
        {
            // get a timestamp of the last user input
            NativeMethods.GetLastInputInfo(ref _lastInputInfo);

            // idle if no input for more than 'Interval' milliseconds (120s)
            var isIdle = ((Environment.TickCount - _lastInputInfo.dwTime) > Settings.NotCountingAsIdleInterval);


            if (isIdle && _lastEntryWasIdle)
            {
                // don't save, already saved
            }
            else if (isIdle && ! _lastEntryWasIdle)
            {
                // save idle
                StoreIdle();
            }
            else if (! isIdle && _lastEntryWasIdle)
            {
                // resumed work in the same program
                StoreProcess(); //TODO: maybe check if not just moved the mouse a little, but actually inserted some data
            }
            else if (! isIdle && ! _lastEntryWasIdle)
            {
                // nothing to do here
            }
        }

        private void StoreIdle()
        {
            var currentWindowTitle = Dict.Idle;
            var process = Dict.Idle;

            if (_lastEntryWasIdle == false)
            {
                _lastEntryWasIdle = true;
                _previousWindowTitleEntry = currentWindowTitle;
                Queries.InsertSnapshot(currentWindowTitle, process);
                //Console.WriteLine(DateTime.Now.ToString("t") + " " + DateTime.Now.Millisecond + "\t" + process + "\t" + currentWindowTitle);
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
        public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            try
            {
                // filter out non-HWND namechanges... (eg. items within a listbox)
                if (idObject != 0 || idChild != 0)
                {
                    return;
                }

                StoreProcess();
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

            // [special case] lockscreen, shutdown, restart
            if (!string.IsNullOrEmpty(currentProcess) && currentProcess.Trim().ToLower(CultureInfo.InvariantCulture).Contains("lockapp"))
            {
                currentWindowTitle = "LockScreen";
                currentProcess = Dict.Idle;
            }
            // [special case]
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
            var differentProcessNotIdle = !string.IsNullOrEmpty(currentProcess) && _previousProcess != currentProcess && currentProcess.Trim().ToLower(CultureInfo.InvariantCulture) != "idle";
            var differentWindowTitle = !string.IsNullOrEmpty(currentWindowTitle) && _previousWindowTitleEntry != currentWindowTitle;
            var notIdleLastInterval = !((Environment.TickCount - _lastInputInfo.dwTime) > Settings.NotCountingAsIdleInterval);

            if ((differentProcessNotIdle || differentWindowTitle) && notIdleLastInterval)
            {
                _previousWindowTitleEntry = currentWindowTitle;
                _previousProcess = currentProcess;
                _lastEntryWasIdle = false;

                Queries.InsertSnapshot(currentWindowTitle, currentProcess);
                //Console.WriteLine(DateTime.Now.ToString("t") + " " + DateTime.Now.Millisecond + "\t" + currentProcess + "\t" + currentWindowTitle);
            }
        }

        /// <summary>
        /// Get the name of the current process
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        private static string GetProcessName(IntPtr handle)
        {
            try
            {
                uint processId;
                NativeMethods.GetWindowThreadProcessId(handle, out processId);
                return Process.GetProcessById((int)processId).ProcessName;
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
