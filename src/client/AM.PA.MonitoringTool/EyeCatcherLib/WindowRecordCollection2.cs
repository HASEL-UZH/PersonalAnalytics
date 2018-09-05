using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EyeCatcherDatabase.Records;
using EyeCatcherLib.Native;

namespace EyeCatcherLib
{
    /// <summary>
    /// A class to manage the windows with the lifecycle events.
    /// 1. Create (always calls ->Activate)
    /// 2. Move, Resize, Deactivate, Minimize, Maximize, Rename
    /// 3. Destroy
    /// 
    /// https://docs.microsoft.com/en-us/windows/desktop/winmsg/about-windows
    /// https://docs.microsoft.com/en-us/windows/desktop/winmsg/using-windows
    /// The first window an application creates is typically the main window. 
    /// The system does not automatically display the main window after creating it.
    /// 
    /// Child Windows
    /// https://docs.microsoft.com/en-us/windows/desktop/winmsg/window-features#child-windows
    /// You can divide a window's client area into different functional areas by using child windows.
    /// Child windows are always opened from within a main or pop-up window, which becomes the child window’s parent.
    /// A child window must have one parent window, but a parent can have any number of child windows. 
    /// The parent window can be an overlapped window, a pop-up window, or even another child window.
    /// - Positioning
    /// A child window has the WS_CHILD style and is confined to the client area of its parent window. No part of a child window ever appears outside the borders of its parent window.
    /// You can move the child window within the parent window, but not outside the parent.
    /// If SetParent specifies a NULL handle, the desktop window becomes the new parent window. In this case, the child window is drawn on the desktop, outside the borders of any other window.
    /// - Behaviour
    /// The parent window draws over the child window if it carries out any drawing in the same location as the child window (Except  WS_CLIPCHILDREN style). Sibling windows can draw in each other's client area.
    /// Child windows cannot have menus and are never considered the active window.
    /// 
    /// </summary>
    public class WindowRecordCollection2 : IWindowRecordCollection
    {
        private readonly IWindowRecordProvider _windowRecordProvider;

        public WindowRecordCollection2(IWindowRecordProvider windowRecordProvider)
        {
            _windowRecordProvider = windowRecordProvider ?? throw new ArgumentNullException(nameof(windowRecordProvider));
        }

        private ConcurrentDictionary<IntPtr, WindowRecord> Windows { get; } = new ConcurrentDictionary<IntPtr, WindowRecord>();
        private IList<IntPtr> Ranks { get; set; } = new List<IntPtr>();

        #region Public

        public IList<WindowRecord> Initialize()
        {
            Update();
            return GetRankedWindowRecords();
        }

        public WindowRecord GetWindowRecord(IntPtr hWnd)
        {
            if (Windows.TryGetValue(hWnd, out var record))
            {
                return record;
            }
            // finding ChildWindows
            if (Windows.TryGetValue(NativeMethods.GetTopLevelParent(hWnd), out var parentRecord))
            {
                return parentRecord.ChildWindows.FirstOrDefault(rec => rec.HWnd == hWnd);
            }
            return null;
        }

        public WindowRecord Activate(IntPtr hwnd)
        {
            if (Ranks.First() != hwnd)
            {
                Update();
            }
            Windows.TryGetValue(hwnd, out var record);
            return record;
        }

        public WindowRecord Destroy(IntPtr hwnd)
        {
            Windows.TryRemove(hwnd, out var record);
            Ranks.Remove(hwnd);
            // no need to update - after every destroy an activate of the following window is done anyway
            return record;
        }

        public WindowRecord MoveOrResize(IntPtr hwnd)
        {
            return Replace(hwnd);
        }

        public WindowRecord Minimize(IntPtr hwnd)
        {
            return Replace(hwnd);
        }

        public WindowRecord MinimizeEnd(IntPtr hwnd)
        {
            return Replace(hwnd);
        }

        public WindowRecord Rename(IntPtr hwnd)
        {
            var newTitle = NativeMethods.GetWindowText(hwnd);
            if (string.IsNullOrWhiteSpace(newTitle) || newTitle == GetWindowRecord(hwnd)?.Title)
            {
                return null;
            }
            return Replace(hwnd);
        }

        public DesktopRecord GetDesktopRecord()
        {
            var list = new List<DesktopWindowLinkRecord>();
            var i = 0;
            // Closure
            var ranks = Ranks;
            foreach (var hwnd in ranks)
            {
                Windows.TryGetValue(hwnd, out var record);
                list.Add(new DesktopWindowLinkRecord
                {
                    Window = record,
                    ZIndex = i++
                });
            }
            return new DesktopRecord { WindowLinks = list };
        }

        public IList<WindowRecord> GetRankedWindowRecords()
        {
            var list = new List<WindowRecord>();
            var ranks = Ranks;
            foreach (var hwnd in ranks)
            {
                Windows.TryGetValue(hwnd, out var record);
                list.Add(record);
            }
            return list;
        }

        #endregion

        /// <summary>
        /// Updating Windows and Ranks
        /// </summary>
        private void Update()
        {
            // https://stackoverflow.com/questions/7277366/why-does-enumwindows-return-more-windows-than-i-expected?utm_medium=organic&utm_source=google_rich_qa&utm_campaign=google_rich_qa
            var newRanks = new List<IntPtr>();
            NativeMethods.EnumWindows((hWnd, param) =>
            {
                var record = GetWindowRecord(hWnd);
                if (record == null)
                {
                    record = _windowRecordProvider.GetWindowRecord(hWnd);
                    if (record == null)
                    {
                        // continue
                        return true;
                    }
                    Windows.TryAdd(hWnd, record);
                }
                newRanks.Add(hWnd);
                return true;
            }, IntPtr.Zero);
            Ranks = newRanks;
        }

        /// <summary>
        /// Updating a WindowRecord and keeping it at the same Rank
        /// </summary>
        /// <param name="hwnd"></param>
        private WindowRecord Replace(IntPtr hwnd)
        {
            var newRecord = _windowRecordProvider.GetWindowRecord(hwnd);
            if (newRecord == null)
            {
                // TODO RR: Why is it null?
                return null;
            }

            try
            {
                Windows[hwnd] = newRecord;
            }
            catch (KeyNotFoundException e)
            {
                // TODO RR: Doesn't exist yet?
                Trace.WriteLine($"Window to Replace {hwnd} does not exist.");
            }
            return newRecord;
        }
    }
}
