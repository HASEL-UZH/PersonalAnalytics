using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowRecommender.Models
{
    internal class MostRecentlyActive : BaseModel
    {
        private List<IntPtr> _windows;

        internal MostRecentlyActive(IWindowEvents windowEvents) : base(windowEvents)
        {
            _windows = new List<IntPtr>();

            windowEvents.WindowClosedOrMinimized += OnWindowClosedOrMinimized;
            windowEvents.WindowOpened += OnWindowOpened;
            windowEvents.WindowFocused += OnWindowFocused;
        }

        public override Dictionary<IntPtr, double> GetScores()
        {
            var scores = new Dictionary<IntPtr, double>();
            for (var i = 0; i < _windows.Count; i++)
            {
                scores[_windows[i]] = i < Settings.NumberOfWindows ? 1 : 0;
            }
            return scores;
        }

        protected override void Setup(List<WindowRecord> windowRecords)
        {
            _windows = windowRecords.Select(record => record.Handle).ToList();
        }

        private void OnWindowClosedOrMinimized(object sender, WindowRecord e)
        {
            var windowRecord = e;
            var index = _windows.IndexOf(windowRecord.Handle);
            if (index != -1)
            {
                var hasChanged = index < Settings.NumberOfWindows;
                _windows.Remove(windowRecord.Handle);
                if (hasChanged)
                {
                    InvokeOrderChanged();
                }
            }
        }

        private void OnWindowFocused(object sender, WindowRecord e)
        {
            var windowRecord = e;
            var index = _windows.IndexOf(windowRecord.Handle);
            var hasChanged = index == -1 || index >= Settings.NumberOfWindows;
            _windows.Remove(windowRecord.Handle);
            _windows.Insert(0, windowRecord.Handle);
            if (hasChanged)
            {
                InvokeOrderChanged();
            }
        }

        private void OnWindowOpened(object sender, WindowRecord e)
        {
            var windowRecord = e;
            _windows.Insert(0, windowRecord.Handle);
            InvokeOrderChanged();
        }
    }
}
