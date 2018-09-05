using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using EyeCatcherDatabase.Records;
using EyeCatcherLib.Native;

namespace EyeCatcherLib
{
    /// <summary>
    /// How to get coordinates of the window client area?
    /// Use ClientToScreen (or Winforms Control.PointToScreen) to get the coordinates of the upper left (0,0) point in screen coordinates. 
    /// The RECT returned by GetClientRect will be appropriate to get you the lower right corner (just add to the POINT set by ClientToScreen).
    /// </summary>
    public class WindowRecordProvider : IWindowRecordProvider
    {
        private readonly IGetScreenRecords _screenRecords;

        public WindowRecordProvider(IGetScreenRecords screenRecords)
        {
            _screenRecords = screenRecords ?? throw new ArgumentNullException(nameof(screenRecords));
        }

        /// <summary>
        /// Only creates a record for TOP LEVEL WINDOWS
        /// </summary>
        public WindowRecord GetWindowRecord(IntPtr hWnd)
        {
            if (!NativeMethods.IsTopLevelWindow(hWnd))
            {
                return null;
            }

            var windowRecord = GetVisibleWindowRecord(hWnd);
            if (windowRecord == null)
            {
                return null;
            }

            // Setting Child Windows
            foreach (var childHwnd in NativeMethods.GetChildWindows(hWnd))
            {
                var childRecord = GetVisibleWindowRecord(childHwnd);
                if (childRecord == null)
                {
                    continue;
                }
                childRecord.ParentWindow = windowRecord;
                windowRecord.ChildWindows.Add(childRecord);
            }

            // Owner?
            var owner = NativeMethods.GetOwnerWindow(hWnd);
            if (owner != IntPtr.Zero)
            {
                windowRecord.OwnerHwndInteger = owner.ToInt64();
            }

            return windowRecord;
        }

        private WindowRecord GetVisibleWindowRecord(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero || !NativeMethods.IsWindowVisible(hWnd))
            {
                return null;
            }

            if (NativeMethods.IsCloakedWindow(hWnd))
            {
                // either on different desktop or hidden windows store app
                return null;
            }

            var rect = NativeMethods.GetWindowRect(hWnd);
            if (rect.Size == Size.Empty)
            {
                return null;
            }

            ScreenRecord screen = null;
            var monitor = NativeMethods.MonitorFromWindow(hWnd);
            if (monitor != IntPtr.Zero)
            {
                screen = _screenRecords.CurrentScreens.FirstOrDefault(s =>s.HMonitor == monitor);
            }

            var windowRecord = new WindowRecord
            {
                HWnd = hWnd,
                HwndInteger = hWnd.ToInt64(),
                Title = NativeMethods.GetWindowText(hWnd),
                ClassName = NativeMethods.GetClassName(hWnd),
                Screen = screen,
                Rectangle = rect
            };
            SetStatus(windowRecord);
            SetProcessInformation(windowRecord);
            return windowRecord;
        }

        private static void SetProcessInformation(WindowRecord windowRecord)
        {
            try
            {
                // throws
                windowRecord.ThreadId = NativeMethods.GetWindowThreadProcessId(windowRecord.HWnd, out var processId);
                windowRecord.ProcessId = processId;
                // Process.GetProcessById throws an exception if the process with this ID is not running.
                windowRecord.Process = Process.GetProcesses().FirstOrDefault(process => process.Id == processId);
                windowRecord.ProcessName = windowRecord.Process?.ProcessName;
            }
            catch
            {
                // ignored
            }
        }

        private static void SetStatus(WindowRecord windowRecord)
        {
            var windowPlacement = NativeMethods.GetWindowPlacement(windowRecord.HWnd);
            switch (windowPlacement.showCmd)
            {
                case ShowWindowCommand.SW_MINIMIZE:
                case ShowWindowCommand.SW_SHOWMINIMIZED:
                    windowRecord.Minimized = true;
                    windowRecord.Maximized = false;
                    break;

                case ShowWindowCommand.SW_SHOWMAXIMIZED:
                    windowRecord.Minimized = false;
                    windowRecord.Maximized = true;
                    break;
            }
            windowRecord.TopMostWindow = NativeMethods.IsWindowTopMost(windowRecord.HWnd);
        }
    }
}
