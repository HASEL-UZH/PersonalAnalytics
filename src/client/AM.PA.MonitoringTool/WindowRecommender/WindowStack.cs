using System.Collections.Generic;

namespace WindowRecommender
{
    internal class WindowStack
    {
        internal List<WindowRecord> WindowRecords { get; private set; }

        internal WindowStack(IWindowEvents windowEvents)
        {
            WindowRecords = new List<WindowRecord>();
            windowEvents.WindowOpened += OnWindowOpened;
            windowEvents.WindowFocused += OnWindowFocused;
            windowEvents.WindowClosedOrMinimized += OnWindowClosedOrMinimized;
            windowEvents.Setup += OnSetup;
        }

        internal int GetZIndex(WindowRecord windowRecord)
        {
            return WindowRecords.IndexOf(windowRecord);
        }

        private void OnSetup(object sender, List<WindowRecord> windowRecords)
        {
            WindowRecords = windowRecords;
        }

        private void OnWindowClosedOrMinimized(object sender, WindowRecord e)
        {
            var windowRecord = e;
            WindowRecords.Remove(windowRecord);
        }

        private void OnWindowFocused(object sender, WindowRecord e)
        {
            var windowRecord = e;
            WindowRecords.Remove(windowRecord);
            WindowRecords.Insert(0, windowRecord);
        }

        private void OnWindowOpened(object sender, WindowRecord e)
        {
            var windowRecord = e;
            WindowRecords.Insert(0, windowRecord);
        }
    }
}
