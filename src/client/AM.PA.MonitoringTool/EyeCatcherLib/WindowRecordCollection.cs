using System;
using System.Collections.Generic;
using System.Linq;
using EyeCatcherDatabase.Records;
using EyeCatcherLib.Native;

namespace EyeCatcherLib
{
    public class WindowRecordCollection : IWindowRecordCollection
    {
        private readonly IWindowRecordProvider _windowRecordProvider;

        public WindowRecordCollection(IWindowRecordProvider windowRecordProvider)
        {
            _windowRecordProvider = windowRecordProvider ?? throw new ArgumentNullException(nameof(windowRecordProvider));
        }

        private IList<WindowRecord> Windows { get; } = new List<WindowRecord>();

        public IList<WindowRecord> Initialize()
        {
            // https://stackoverflow.com/questions/7277366/why-does-enumwindows-return-more-windows-than-i-expected?utm_medium=organic&utm_source=google_rich_qa&utm_campaign=google_rich_qa
            NativeMethods.EnumWindows((hWnd, param) =>
            {
                var windowRecord = _windowRecordProvider.GetWindowRecord(hWnd);
                if (windowRecord != null)
                {
                    Windows.Add(windowRecord);
                }
                return true;
            }, IntPtr.Zero);
            return Windows;
        }

        #region Public

        public WindowRecord GetWindowRecord(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                return null;
            }

            var record = GetTopLevelWindowRecord(hWnd);
            if (record != null)
            {
                return record;
            }

            // finding ChildWindows
            var parentHwnd = NativeMethods.GetTopLevelParent(hWnd);
            var parentRecord = Windows.FirstOrDefault(wi => wi.HWnd == parentHwnd);
            return parentRecord?.ChildWindows.FirstOrDefault(rec => rec.HWnd == hWnd);
        }

        private WindowRecord GetTopLevelWindowRecord(IntPtr hwnd)
        {
            return hwnd == IntPtr.Zero ? null : Windows.FirstOrDefault(wi => wi.HWnd == hwnd);
        }

        public WindowRecord Activate(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return null;
            }
            var windowRecord = Windows.FirstOrDefault(wi => wi.HWnd == hwnd);
            if (windowRecord != null)
            {
                Windows.Remove(windowRecord);
            }
            else
            {
                windowRecord = _windowRecordProvider.GetWindowRecord(hwnd);
                if (windowRecord == null)
                {
                    return null;
                }
                windowRecord.Activated = DateTime.Now;
                // TODO RR: Activate Child window?
            }
            Windows.Insert(0, windowRecord);
            return windowRecord;
        }

        public WindowRecord Destroy(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return null;
            }
            var windowInfo = Windows.FirstOrDefault(wi => wi.HWnd == hwnd);
            if (windowInfo == null)
            {
                return null;
                // TODO RR: Destroy Child window?
            }
            Windows.Remove(windowInfo);
            return windowInfo;
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
            if (hwnd == IntPtr.Zero)
            {
                return null;
            }

            var record = GetTopLevelWindowRecord(hwnd);
            if (record == null)
            {
                return null;
            }
            var newTitle = NativeMethods.GetWindowText(hwnd);
            if (string.IsNullOrWhiteSpace(newTitle) || newTitle == record.Title)
            {
                // Rename called twice
                return null;
            }

            // TODO RR: RenameChildWindow?
            return Replace(hwnd);
        }

        public DesktopRecord GetDesktopRecord()
        {
            // TODO RR: Check CollectionChanged etc
            var desktopWindowLinkRecords = Windows.Select((r, i) => new DesktopWindowLinkRecord
            {
                Window = r,
                ZIndex = i
            }).ToList();
            return new DesktopRecord { WindowLinks = desktopWindowLinkRecords };
        }

        #endregion

        /// <summary>
        /// Updating a WindowRecord
        /// </summary>
        /// <param name="hwnd"></param>
        private WindowRecord Replace(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return null;
            }
            for (var i = 0; i < Windows.Count; i++)
            {
                var windowRecord = Windows[i];
                if (windowRecord.HWnd != hwnd)
                {
                    continue;
                }
                // Copy WindowRecord
                var newRecord = _windowRecordProvider.GetWindowRecord(hwnd);
                if (newRecord == null)
                {
                    return null;
                }
                Windows[i] = newRecord;
                return newRecord;
            }
            return null;
        }
    }
}
