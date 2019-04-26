using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowRecommender
{
    internal class WindowStack
    {
        internal List<IntPtr> Windows { get; private set; }

        internal WindowStack(IWindowEvents windowEvents)
        {
            Windows = new List<IntPtr>();
            windowEvents.WindowOpened += OnWindowOpened;
            windowEvents.WindowFocused += OnWindowFocused;
            windowEvents.WindowClosedOrMinimized += OnWindowClosedOrMinimized;
            windowEvents.Setup += OnSetup;
        }

        internal int GetZIndex(IntPtr windowHandle)
        {
            return Windows.IndexOf(windowHandle);
        }

        private void OnSetup(object sender, List<WindowRecord> windowRecords)
        {
            Windows = windowRecords.Select(record => record.Handle).ToList();
        }

        private void OnWindowClosedOrMinimized(object sender, WindowRecord e)
        {
            var windowRecord = e;
            Windows.Remove(windowRecord.Handle);
        }

        private void OnWindowFocused(object sender, WindowRecord e)
        {
            var windowRecord = e;
            Windows.Remove(windowRecord.Handle);
            Windows.Insert(0, windowRecord.Handle);
        }

        private void OnWindowOpened(object sender, WindowRecord e)
        {
            var windowRecord = e;
            Windows.Insert(0, windowRecord.Handle);
        }
    }
}
