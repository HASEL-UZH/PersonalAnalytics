using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

// ReSharper disable InconsistentNaming

namespace EyeCatcherLib.Native
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        // Window

        #region GetDesktopWindow

        /// <summary>
        /// Retrieves a handle to the desktop window. The desktop window covers the entire screen. The desktop window is the area on top of which other windows are painted.
        /// </summary>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = false)]
        private static extern IntPtr GetDesktopWindow();

        #endregion

        #region IsChild

        /// <summary>
        /// Determines whether a window is a child window or descendant window of a specified parent window. 
        /// A child window is the direct descendant of a specified parent window if that parent window is in the chain of parent windows; 
        /// the chain of parent windows leads from the original overlapped or pop-up window to the child window.
        /// </summary>
        /// <param name="hWndParent">A handle to the parent window.</param>
        /// <param name="hWnd">A handle to the window to be tested.</param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        private static extern bool IsChild(IntPtr hWndParent, IntPtr hWnd);

        #endregion

        #region GetAncestor

        private enum GetAncestorFlags
        {
            /// <summary>
            /// Retrieves the parent window. This does not include the owner, as it does with the GetParent function. 
            /// </summary>
            GetParent = 1,
            /// <summary>
            /// Retrieves the root window by walking the chain of parent windows.
            /// </summary>
            GetRoot = 2,
            /// <summary>
            /// Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent. 
            /// </summary>
            GetRootOwner = 3
        }

        /// <summary>
        /// Retrieves the handle to the ancestor of the specified window. 
        /// </summary>
        /// <param name="hwnd">A handle to the window whose ancestor is to be retrieved. 
        /// If this parameter is the desktop window, the function returns NULL. </param>
        /// <param name="flags">The ancestor to be retrieved.</param>
        /// <returns>The return value is the handle to the ancestor window.</returns>
        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flags);

        private static IntPtr GetRealParent(IntPtr hWnd)
        {
            var hParent = GetAncestor(hWnd, GetAncestorFlags.GetParent);
            // The parent of a top-level window is the desktop window.
            if (hParent == IntPtr.Zero || hParent == GetDesktopWindow())
            {
                return IntPtr.Zero;
            }
            return hParent;
        }


        /// <summary>
        /// A Window is a TopLevelWindow if it has no parent.
        /// </summary>
        public static bool IsTopLevelWindow(IntPtr hwnd)
        {
            return GetRealParent(hwnd) == IntPtr.Zero;
        }


        /// <summary>
        /// TODO : Way #2: Use IsTopLevelWindow (user32 Win7, undocumented)
        /// WS_OVERLAPPED and WS_POPUP indicate a top level window.
        /// WS_OVERLAPPED constant is 0, it does not make a good mask.  But all
        /// WS_OVERLAPPED windows MUST have a caption so use WS_CAPTION instead.
        /// </summary>
        public static IntPtr GetTopLevelParent(IntPtr hwnd)
        {
            while (true)
            {
                var parent = GetRealParent(hwnd);
                if (parent == IntPtr.Zero)
                {
                    return hwnd;
                }
                hwnd = parent;
            }
        }

        #endregion

        #region GetWindow

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern IntPtr GetWindow(IntPtr hwnd, uint uCmd);

        public static IntPtr GetOwnerWindow(IntPtr hwnd)
        {
            const uint GW_OWNER = 4;
            return GetWindow(hwnd, GW_OWNER);
        }

        #endregion

        #region GetParent

        /// <summary>
        /// https://blogs.msdn.microsoft.com/oldnewthing/20111207-00/?p=8953/
        /// The Get­Parent function returns the parent window, or owner window, or possibly neither:
        /// If the window is a child window, then return the parent window. Else, the window is a top-level window.If WS_POPUP style is set, and the window has an owner, then return the owner. Else, return NULL.
        /// 
        /// To get the parent window, call GetAncestor(hwnd, GA_PARENT).
        /// To get the owner window, call GetWindow(hwnd, GW_OWNER).
        /// </summary>
        [Obsolete("Use GetAncestor")]
        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        private static extern IntPtr GetParent(IntPtr hWnd);

        #endregion

        #region EnumWindows

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        /// <summary>
        /// Enumerates all top-level windows on the screen by passing the handle to each window
        /// https://stackoverflow.com/questions/7277366/why-does-enumwindows-return-more-windows-than-i-expected?utm_medium=organic&utm_source=google_rich_qa&utm_campaign=google_rich_qa
        /// </summary>
        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        /// <summary>
        /// The EnumChildWindows function enumerates the child windows of a parent window. 
        /// Then, EnumChildWindows passes the handle to each child window to an application-defined callback function.
        /// If a child window has created child windows of its own, EnumChildWindows enumerates those windows as well.
        /// 
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        /// <summary>
        /// Returns a list of child windows
        /// </summary>
        /// <param name="parent">Parent of the windows to return</param>
        /// <returns>List of child windows</returns>
        public static List<IntPtr> GetChildWindows(IntPtr parent)
        {
            var result = new List<IntPtr>();
            var listHandle = GCHandle.Alloc(result);
            try
            {
                var childProc = new EnumWindowsProc(EnumWindow);
                EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                {
                    listHandle.Free();
                }
            }
            return result;
        }

        /// <summary>
        /// Callback method to be used when enumerating windows.
        /// </summary>
        /// <param name="handle">Handle of the next window</param>
        /// <param name="pointer">Pointer to a GCHandle that holds a reference to the list to fill</param>
        /// <returns>True to continue the enumeration, false to bail</returns>
        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            var gch = GCHandle.FromIntPtr(pointer);
            if (!(gch.Target is List<IntPtr> list))
            {
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            }
            list.Add(handle);
            //  You can modify this to check to see if you want to cancel the operation, then return a null here
            return true;
        }
        
        #endregion

        #region IsWindow

        /// <summary>
        /// Checks if the handle is a window
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool IsWindow(IntPtr hwnd);

        #endregion

        #region IsWindowVisible

        /// <summary>
        /// If the specified window, its parent window, its parent's parent window, and so forth, have the WS_VISIBLE style, the return value is nonzero. Otherwise, the return value is zero.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns>return value specifies whether the window has the WS_VISIBLE style. it may be true even if the window is totally obscured by other windows.</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        #endregion

        #region IsIconic

        /// <summary>
        /// Determines whether the specified window is minimized (iconic).
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr hWnd);

        #endregion

        #region IsZoomed

        /// <summary>
        /// Determines whether a window is maximized.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsZoomed(IntPtr hWnd);

        #endregion

        #region WindowFromPoint

        /// <summary>
        /// Retrieves a handle to the window that contains the specified point.
        /// 
        /// The WindowFromPoint function does not retrieve a handle to a hidden or disabled window, even if the point is within the window. An application should use the ChildWindowFromPoint function for a nonrestrictive search.
        /// </summary>
        /// <param name="p"></param>
        /// <returns>
        /// The return value is a handle to the window that contains the point. If no window exists at the given point, the return value is NULL. If the point is over a static text control, the return value is a handle to the window under the static text control
        /// </returns>
        /// <remarks>
        /// System.Drawing.Point, the order of the fields in System.Drawing.Point isn't guaranteed to stay the same.
        /// </remarks>
        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT p);

        #endregion

        #region GetWindowPlacement

        /// <summary>
        /// Retrieves the show state and the restored, minimized, and maximized positions of the specified window.
        /// </summary>
        /// <param name="hWnd">
        /// A handle to the window.
        /// </param>
        /// <param name="lpwndpl">
        /// A pointer to the WindowPlacement structure that receives the show state and position information.
        /// <para>
        /// Before calling GetWindowPlacement, set the length member to sizeof(WindowPlacement). GetWindowPlacement fails if lpwndpl-> length is not set correctly.
        /// </para>
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero.
        /// <para>
        /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
        /// </para>
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement lpwndpl);

        public static WindowPlacement GetWindowPlacement(IntPtr hwnd)
        {
            var placement = new WindowPlacement();
            placement.length = (uint)Marshal.SizeOf(placement);
            GetWindowPlacement(hwnd, ref placement);
            return placement;
        }


        #endregion

        #region GetWindowLong

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GWL_EXSTYLE = -20;
        private const int GWL_STYLE = -16;
        private const int WS_EX_TOPMOST = 0x0008;
        internal const int GWL_ID = -12;
        internal const int GWL_HWNDPARENT = -8;
        internal const int GWL_WNDPROC = -4;

        // Gets the style of the window
        internal static int GetWindowStyle(IntPtr hwnd)
        {
            var style = GetWindowLong(hwnd, GWL_STYLE);
            return style;
        }

        public static bool IsWindowTopMost(IntPtr hWnd)
        {
            var exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            return (exStyle & WS_EX_TOPMOST) == WS_EX_TOPMOST;
        }

        #endregion

        #region GetWindowText

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        public static string GetWindowText(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                return string.Empty;
            }

            const int maxChars = 256;
            var stringBuilder = new StringBuilder(maxChars);
            return GetWindowText(hWnd, stringBuilder, maxChars) > 0 ? stringBuilder.ToString() : string.Empty;
        }

        #endregion

        #region GetWindowThreadProcessId

        /// <summary>
        /// Gets Thread and Process Id for a window
        /// 
        /// Be aware that this seems to throw an exception
        /// A 32 bit processes cannot access modules of a 64 bit process”) when run from a 32-bit application when the active process is 64-bit.
        /// </summary>
        /// <param name="hWnd">The widnow handle</param>
        /// <param name="processId">The process id</param>
        /// <returns>The thread id</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        #endregion

        #region GetWindowRect

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        public static Rectangle GetWindowRect(IntPtr hwnd)
        {
            if (GetWindowRect(hwnd, out var rct))
            {
                return (Rectangle)rct;
            }
            return Rectangle.Empty;
        }

        #endregion

        #region GetClassName

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        public static string GetClassName(IntPtr hWnd)
        {
            // Pre-allocate 256 characters, since this is the maximum class name length.
            var className = new StringBuilder(256);
            //Get the window class name
            GetClassName(hWnd, className, className.Capacity);
            return className.ToString();
        }

        #endregion

        #region DwmGetWindowAttribute

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/desktop/api/dwmapi/ne-dwmapi-dwmwindowattribute
        /// </summary>
        private enum DWMWINDOWATTRIBUTE : uint
        {
            /// <summary>
            /// Use with DwmGetWindowAttribute. 
            /// Discovers whether non-client rendering is enabled. 
            /// The retrieved value is of type BOOL. TRUE if non-client rendering is enabled; otherwise, FALSE.
            /// </summary>
            DWMWA_NCRENDERING_ENABLED = 1,
            DWMWA_NCRENDERING_POLICY,
            DWMWA_TRANSITIONS_FORCEDISABLED,
            DWMWA_ALLOW_NCPAINT,
            DWMWA_CAPTION_BUTTON_BOUNDS,
            DWMWA_NONCLIENT_RTL_LAYOUT,
            DWMWA_FORCE_ICONIC_REPRESENTATION,
            DWMWA_FLIP3D_POLICY,
            DWMWA_EXTENDED_FRAME_BOUNDS,
            DWMWA_HAS_ICONIC_BITMAP,
            DWMWA_DISALLOW_PEEK,
            DWMWA_EXCLUDED_FROM_PEEK,
            /// <summary>
            /// Use with DwmSetWindowAttribute. Cloaks the window such that it is not visible to the user. The window is still composed by DWM.
            /// Windows 7 and earlier:  This value is not supported.
            /// </summary>
            DWMWA_CLOAK,
            /// <summary>
            /// Use with DwmGetWindowAttribute. If the window is cloaked, provides one of the <see cref="DWMCLOAKEDRESULT"/> values explaining why.
            /// Windows 7 and earlier:  This value is not supported.
            /// </summary>
            DWMWA_CLOAKED,
            /// <summary>
            /// Use with DwmSetWindowAttribute. Freeze the window's thumbnail image with its current visuals. Do no further live updates on the thumbnail image to match the window's contents.
            /// </summary>
            FreezeRepresentation,
            DWMWA_TABBING_ENABLED,
            DWMWA_ASSOCIATED_WINDOW,
            DWMWA_TAB_GROUPING_PREFERENCE,
            /// <summary>
            /// The maximum recognized DWMWINDOWATTRIBUTE value, used for validation purposes.
            /// </summary>
            DWMWA_LAST
        }

        private enum DWMCLOAKEDRESULT : int
        {
            NOT_CLOAKED = 0x0000000,
            /// <summary>
            /// The window was cloaked by its owner application.
            /// </summary>
            DWM_CLOAKED_APP = 0x0000001,
            /// <summary>
            /// The window was cloaked by the Shell.
            /// </summary>
            DWM_CLOAKED_SHELL = 0x0000002,
            /// <summary>
            /// The cloak value was inherited from its owner window.
            /// </summary>
            DWM_CLOAKED_INHERITED = 0x0000004
        }

        /// <summary>
        /// Retrieves the current value of a specified attribute applied to a window.
        /// </summary>
        /// <param name="hwnd">The handle to the window from which the attribute data is retrieved.</param>
        /// <param name="dwAttribute">The attribute to retrieve, specified as a <see cref="DWMWINDOWATTRIBUTE"/> value.</param>
        /// <param name="pvAttribute">
        /// A pointer to a value that, when this function returns successfully, receives the current value of the attribute. 
        /// The type of the retrieved value depends on the value of the dwAttribute parameter.
        /// </param>
        /// <param name="cbAttribute">
        /// The size of the DWMWINDOWATTRIBUTE value being retrieved. The size is dependent on the type of the pvAttribute parameter.
        /// </param>
        /// <returns>If this function succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.</returns>
        [DllImport("dwmapi.dll")]
        static extern int DwmGetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, out bool pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        static extern int DwmGetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, out RECT pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        static extern int DwmGetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, out DWMCLOAKEDRESULT pvAttribute, int cbAttribute);

        public const int S_OK = 0x00000000;

        public static bool IsCloakedWindow(IntPtr hWnd)
        {
            var size = Marshal.SizeOf(Enum.GetUnderlyingType(typeof(DWMCLOAKEDRESULT)));
            var hRes = DwmGetWindowAttribute(hWnd, DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, out DWMCLOAKEDRESULT cloakedResult, size);
            if (hRes != S_OK)
            {
                // error ... probebly not cloaked?
                return false;
            }
            return cloakedResult != DWMCLOAKEDRESULT.NOT_CLOAKED;
        }

        #endregion

        // Monitor

        #region MonitorFromWindow

        private const int MONITOR_DEFAULTTONULL = 0;
        private const int MONITOR_DEFAULTTOPRIMARY = 1;
        private const int MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        public static IntPtr MonitorFromWindow(IntPtr hwnd)
        {
            return MonitorFromWindow(hwnd, MONITOR_DEFAULTTONULL);
        }

        #endregion

        #region GetMonitorInfo

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-getmonitorinfoa
        /// The GetMonitorInfo function retrieves information about a display monitor.
        /// </summary>
        /// <param name="hMonitor">A handle to the display monitor of interest.</param>
        /// <param name="lpmi">
        /// A pointer to a MONITORINFO or MONITORINFOEX structure that receives information about the specified display monitor.
        /// You must set the cbSize member of the structure to sizeof(MONITORINFO) or sizeof(MONITORINFOEX) before calling the GetMonitorInfo function.Doing so lets the function determine the type of structure you are passing to it.
        ///
        /// The MONITORINFOEX structure is a superset of the MONITORINFO structure. It has one additional member: a string that contains a name for the display monitor.Most applications have no use for a display monitor name, and so can save some bytes by using a MONITORINFO structure.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero.
        /// If the function fails, the return value is zero.
        /// </returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        public static MONITORINFOEX GetMonitorInfo(IntPtr hMonitor)
        {
            var monitorinfoex = new MONITORINFOEX();
            monitorinfoex.cbSize = Marshal.SizeOf(monitorinfoex);
            monitorinfoex.DeviceName = string.Empty;
            // TODO RR: What if the call fails?
            GetMonitorInfo(hMonitor, ref monitorinfoex);
            return monitorinfoex;
        }

        #endregion

        #region EnumDisplayMonitors

        internal delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-enumdisplaymonitors
        /// The EnumDisplayMonitors function enumerates display monitors (including invisible pseudo-monitors associated with the mirroring drivers) that intersect a region formed by the intersection of a specified clipping rectangle and the visible region of a device context.
        /// EnumDisplayMonitors calls an application-defined MonitorEnumProc callback function once for each monitor that is enumerated.
        /// Note that GetSystemMetrics (SM_CMONITORS) counts only the display monitors.
        /// </summary>
        /// <param name="hdc">A handle to a display device context that defines the visible region of interest. If this parameter is NULL, the hdcMonitor parameter passed to the callback function will be NULL, and the visible region of interest is the virtual screen that encompasses all the displays on the desktop.</param>
        /// <param name="lprcClip">A pointer to a RECT structure that specifies a clipping rectangle. The region of interest is the intersection of the clipping rectangle with the visible region specified by hdc.</param>
        /// <param name="lpfnEnum">A pointer to a MonitorEnumProc application-defined callback function.</param>
        /// <param name="dwData">Application-defined data that EnumDisplayMonitors passes directly to the MonitorEnumProc function.</param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        internal static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        public static IList<IntPtr> GetAllMonitors()
        {
            var result = new List<IntPtr>();
            var listHandle = GCHandle.Alloc(result);
            try
            {
                var monitorEnumProc = new MonitorEnumProc(MonitorEnumCallBack);
                EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, monitorEnumProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                {
                    listHandle.Free();
                }
            }
            return result;
        }

        private static bool MonitorEnumCallBack(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
        {
            var gch = GCHandle.FromIntPtr(dwData);
            if (!(gch.Target is List<IntPtr> list))
            {
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            }

            list.Add(hMonitor);
            return true;
        }

        #endregion

        // IO

        #region GetCursorPos

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out POINT lpPoint);

        #endregion

        #region GetLastInputInfo

        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        /// <summary>
        /// Get the The tick count when the last input event was received.
        /// </summary>
        public static uint GetLastInputTick()
        {
            var lastinputinfo = new LASTINPUTINFO();
            lastinputinfo.cbSize = (uint)Marshal.SizeOf(lastinputinfo);
            if (!GetLastInputInfo(ref lastinputinfo))
            {
                // TODO RR: Error handling
                return 0;
            }
            return lastinputinfo.dwTime;
        }
        #endregion

    }
}
