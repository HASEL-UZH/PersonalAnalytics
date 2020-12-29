// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using MouseKeyboardActivityMonitor;
using System.Windows.Forms;
using MouseKeyboardActivityMonitor.WinApi;

namespace WindowsContextTracker.Helpers
{
    /// <summary>
    /// A helper class that tracks a users changes. If at least one of the following
    /// conditions applies, the new screenshot is also being processed. Otherwise, it
    /// is deleted and the tracker waits for the next interval's screenshot.
    /// 
    /// The conditions are:
    /// - A change is if the user changes the window/programe he is using.
    /// - A change is if the user types some text (above a threshold).
    /// - A change is if the user scrolls through text (above a threshold).
    /// </summary>
    public class ScreenshotChangedTracker
    {
        #region FIELDS

        private string _previousWindowTitle;
        private string _previousProcessName;

        private MouseHookListener _mouseListener;
        private int _totalMouseDistanceScrolled;
        private int _totalMouseClicks;

        private KeyboardHookListener _keyboardListener;
        private int _totalKeystrokes;

        #endregion

        #region METHODS

        /// <summary>
        /// register mouse & keyboard events
        /// </summary>
        public ScreenshotChangedTracker()
        {
            // Register Hooks for Mouse
            _mouseListener = new MouseHookListener(new GlobalHooker());
            _mouseListener.Enabled = true;
            _mouseListener.MouseWheel += MouseListener_MouseScrolling;
            _mouseListener.MouseClick += MouseListener_MouseClicking;

            // Register Hooks for Keyboard
            _keyboardListener = new KeyboardHookListener(new GlobalHooker());
            _keyboardListener.Enabled = true;
            _keyboardListener.KeyDown += KeyboardListener_KeyDown;
        }

        /// <summary>
        /// unregister mouse & keyboard events
        /// </summary>
        ~ScreenshotChangedTracker()
        {
            _mouseListener.MouseWheel -= MouseListener_MouseScrolling;
            _mouseListener.MouseClick -= MouseListener_MouseClicking;
            _mouseListener.Stop();
            _mouseListener = null;

            _keyboardListener.KeyDown -= KeyboardListener_KeyDown;
            _keyboardListener.Stop();
            _keyboardListener = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool WindowChanged()
        {
            var didChange = ChangeAsOfWindowChanged() || ChangeAsOfMajorTyping() || ChangeAsOfMajorScrolling() || ChangeAsOfMajorClicking();

            ResetVariables();
            return didChange;
        }

        /// <summary>
        /// Reset everything to track for next interval
        /// </summary>
        private void ResetVariables()
        {
            _totalMouseDistanceScrolled = 0;
            _totalKeystrokes = 0;
            _totalMouseClicks = 0;
        }

        #region Change Evaluators

        /// <summary>
        /// Change if user scrolled more than threashold
        /// </summary>
        /// <returns></returns>
        private bool ChangeAsOfMajorScrolling()
        {
            return (_totalMouseDistanceScrolled > Settings.DistanceOfScrollingThreshold);
        }

        /// <summary>
        /// Change if user typed more than threshold
        /// </summary>
        /// <returns></returns>
        private bool ChangeAsOfMajorTyping()
        {
            return _totalKeystrokes > Settings.NumberOfCharsTypedThreshold;
        }

        /// <summary>
        /// Change if user clicked more than threshold
        /// </summary>
        /// <returns></returns>
        private bool ChangeAsOfMajorClicking()
        {
            return _totalMouseClicks > Settings.NumberOfClicksThreshold;
        }

        /// <summary>
        /// Change if window changed
        /// </summary>
        private bool ChangeAsOfWindowChanged()
        {

            var currentWindowTitle = GetWindowText();
            var currentProcessName = GetProcessName();

            var processChanged = currentProcessName != _previousProcessName;
            var titleChanged = currentWindowTitle != _previousWindowTitle;

            var windowChanged = processChanged || titleChanged;

            _previousProcessName = currentProcessName;
            _previousWindowTitle = currentWindowTitle;

            return windowChanged;        
        }

        /// <summary>
        /// This method gets the foreground window and returns the window title if available.
        /// (also used to store OCR info)
        /// </summary>
        /// <returns></returns>
        public string GetWindowText()
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
            {
                return String.Empty;
            }
            else
            {
                return GetWindowText(hwnd);
            }
        }

        /// <summary>
        /// This method gets the foreground window and returns the process name if available.
        /// (also used to store OCR info)
        /// </summary>
        /// <returns></returns>
        public string GetProcessName()
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
            {
                return String.Empty;
            }
            else
            {
                uint processId;
                GetWindowThreadProcessId(hwnd, out processId);

                return GetProcessName(processId);
            }
        }

        #endregion

        #region Helper to get Mouse & Keyboard events

        /// <summary>
        /// sums up the number of keystrokes within a time interval
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeyboardListener_KeyDown(object sender, KeyEventArgs e)
        {
            _totalKeystrokes++;
        }

        /// <summary>
        /// sums up the distance scrolled within a time interval
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MouseListener_MouseScrolling(object sender, MouseEventArgs e)
        {
            _totalMouseDistanceScrolled += Math.Abs(e.Delta);
        }

        /// <summary>
        /// sums up the mouse clicks within a time interval
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MouseListener_MouseClicking(object sender, MouseEventArgs e)
        {
            _totalMouseClicks++;
        }

        #endregion

        #region Helper to get the Current Window Name & Process

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        /// <summary>
        /// Gets the window title
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns></returns>
        private static string GetWindowText(IntPtr hwnd)
        {
            var length = GetWindowTextLength(hwnd);
            var buffer = new StringBuilder(length + 1);
            GetWindowText(hwnd, buffer, buffer.Capacity);
            return buffer.ToString();
        }

        /// <summary>
        /// Returns the name of the current process, given the id of the process
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        private static string GetProcessName(uint processId)
        {
            var p = Process.GetProcessById((int)processId);
            return p.ProcessName;
        }

        #endregion

        #endregion
    }
}
