// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2017-07-20
// 
// Licensed under the MIT License.

using Shared;
using System;

namespace WindowsActivityTracker.Models
{
    /// <summary>
    /// Helper class to handle the previous entry values
    /// </summary>
    internal class WindowsActivityEntry
    {
        public WindowsActivityEntry(DateTime tsStart, DateTime tsEnd, string windowTitle, string process, IntPtr handle)
        {
            TsStart = tsStart;
            TsEnd = tsEnd;
            WindowTitle = windowTitle;
            Process = process;
            Handle = handle;
        }

        public WindowsActivityEntry(DateTime tsStart, string windowTitle, string process, IntPtr handle)
        {
            TsStart = tsStart;
            TsEnd = DateTime.MinValue;
            WindowTitle = windowTitle;
            Process = process;
            Handle = handle;
        }

        public DateTime TsStart { get; set; }
        public DateTime TsEnd { get; set; }
        public string WindowTitle { get; set; }
        public string Process { get; set; }
        public IntPtr Handle { get; set; }
        public bool WasIdle { get { return (Process == Dict.Idle); } }
    }
}
