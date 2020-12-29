// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-11-04
// 
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowsActivityTracker.Helpers
{
    /// <summary>
    /// Windows OS Event Hooks
    /// 
    /// Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/ms644990(v=vs.85).aspx
    /// GetForegroundWindow Switch: http://stackoverflow.com/questions/8840926/asynchronously-getforegroundwindow-via-sendmessage-or-something/8845757#8845757
    /// GetWindowTitleSwitch: http://stackoverflow.com/questions/9665579/setting-up-hook-on-windows-messages
    /// </summary>
    public static class User32
    {
        public const uint WINEVENT_OUTOFCONTEXT = 0;
        public const uint EVENT_SYSTEM_FOREGROUND = 3;
        public const uint EVENT_OBJECT_NAMECHANGE = 0x800C;

        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [StructLayout(LayoutKind.Sequential)]
        public struct LASTINPUTINFO
        {
            private static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dwTime;
        }
        [DllImport("user32.dll")]
        public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
    }
}
