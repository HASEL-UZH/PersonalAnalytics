using System;
using System.Collections.Generic;

namespace WindowRecommender
{
    internal class WindowStack
    {
        internal List<IntPtr> Windows { get; set; }

        internal WindowStack(ModelEvents modelEvents)
        {
            Windows = new List<IntPtr>();
            modelEvents.WindowFocused += OnWindowFocused;
            modelEvents.WindowClosed += OnWindowClosed;
        }

        internal int GetZIndex(IntPtr windowHandle)
        {
            return Windows.IndexOf(windowHandle);
        }

        private void OnWindowClosed(object sender, IntPtr e)
        {
            var windowHandle = e;
            Windows.Remove(windowHandle);
        }

        private void OnWindowFocused(object sender, IntPtr e)
        {
            var windowHandle = e;
            Windows.Remove(windowHandle);
            Windows.Insert(0, windowHandle);
        }
    }
}
