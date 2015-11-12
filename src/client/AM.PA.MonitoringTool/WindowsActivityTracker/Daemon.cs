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

namespace WindowsActivityTracker
{
    /// <summary>
    /// This tracker stores all program switches (window switch) and changes of the
    /// window titles in the database (using Windows Hooks and its events).
    /// </summary>
    public class Daemon : BaseTracker, ITracker
    {
        User32.WinEventDelegate _dele; // to ensure it's not collected while using
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

        public override void Start()
        {
            try
            {
                // Register for Window Events
                _dele = new User32.WinEventDelegate(WinEventProc);
                _hWinEventHookForWindowSwitch = User32.SetWinEventHook(User32.EVENT_SYSTEM_FOREGROUND, User32.EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _dele, 0, 0, User32.WINEVENT_OUTOFCONTEXT);
                _hWinEventHookForWindowTitleChange = User32.SetWinEventHook(User32.EVENT_OBJECT_NAMECHANGE, User32.EVENT_OBJECT_NAMECHANGE, IntPtr.Zero, _dele, 0, 0, User32.WINEVENT_OUTOFCONTEXT);

                // Register to check if idle or not
                if (Settings.RecordIdle)
                {
                    if (_idleCheckTimer != null)
                        Stop();
                    _idleCheckTimer = new Timer();
                    _idleCheckTimer.Interval = Settings.IdleTimerIntervalInMilliseconds;
                    _idleCheckTimer.Elapsed += CheckIfIdleTime;
                    _idleCheckTimer.Start();

                    _lastInputInfo = new User32.LASTINPUTINFO();
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
                // unregister for window events
                User32.UnhookWinEvent(_hWinEventHookForWindowSwitch);
                User32.UnhookWinEvent(_hWinEventHookForWindowTitleChange);

                // unregister idle time checker
                if (_idleCheckTimer != null)
                {
                    _idleCheckTimer.Stop();
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
            Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.DbTable + " (id INTEGER PRIMARY KEY, time TEXT, window TEXT, process TEXT)");
        }

        public override bool IsEnabled()
        {
            return Settings.IsEnabled;
        }

        #endregion

        /// <summary>
        /// Saves the timestamp, process name and window title into the database.
        /// 
        /// In case the user doesn't want the window title to be stored (For privacy reasons),
        /// it is obfuscated.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="process"></param>
        internal void InsertSnapshot(string window, string process)
        {
            if (!Settings.RecordWindowTitles)
            {
                var dto = new ContextDto { Context = new ContextInfos {ProgramInUse = process, WindowTitle = window} };
                window = "[anonymized] " + ContextMapper.GetContextCategory(dto);  // obfuscate window title
            }

            Database.GetInstance().ExecuteDefaultQuery("INSERT INTO " + Settings.DbTable + " (time, window, process) VALUES (strftime('%Y-%m-%d %H:%M:%f', 'now', 'localtime'), " +
                Database.GetInstance().Q(window) + ", " + Database.GetInstance().Q(process) + ")");
        }

        #region Idle Time Checker

        private User32.LASTINPUTINFO _lastInputInfo;

        /// <summary>
        ///  check every 10 seconds if the user has been idle for the past 120s
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckIfIdleTime(object sender, ElapsedEventArgs e)
        {
            // get a timestamp of the last user input
            User32.GetLastInputInfo(ref _lastInputInfo);

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
                InsertSnapshot(currentWindowTitle, process);
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
            handle = User32.GetForegroundWindow();
            var currentWindowTitle = GetActiveWindowTitle(handle);

            // get current process name
            var currentProcess = GetProcessName(handle);

            // special cases: lockscreen, shutdown, restart
            if (!string.IsNullOrEmpty(currentProcess) && currentProcess.Trim().ToLower().Contains("lockapp"))
            {
                currentProcess = "LockScreen";
                currentWindowTitle = Dict.Idle;
            }
            //TODO: add more special cases

            if ((!string.IsNullOrEmpty(currentProcess) && _previousProcess != currentProcess && currentProcess.Trim().ToLower() != "idle") ||
                (!string.IsNullOrEmpty(currentWindowTitle) && _previousWindowTitleEntry != currentWindowTitle))
            {
                _previousWindowTitleEntry = currentWindowTitle;
                _previousProcess = currentProcess;
                _lastEntryWasIdle = false;

                InsertSnapshot(currentWindowTitle, currentProcess);
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
                User32.GetWindowThreadProcessId(handle, out processId);
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

                if (handle != IntPtr.Zero && User32.GetWindowText(handle, buff, nChars) > 0)
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
