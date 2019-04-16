using System;
using System.Collections.Generic;

namespace WindowRecommender.Models
{
    internal class MostRecentlyActive : BaseModel
    {
        private List<IntPtr> _windows;

        internal MostRecentlyActive(ModelEvents modelEvents) : base(modelEvents)
        {
            _windows = new List<IntPtr>();
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

        public override void SetWindows(List<IntPtr> windows)
        {
            _windows = windows;
        }

        protected override void OnWindowClosed(object sender, IntPtr e)
        {
            var index = _windows.IndexOf(e);
            if (index != -1)
            {
                var hasChanged = index < Settings.NumberOfWindows;
                _windows.Remove(e);
                if (hasChanged)
                {
                    InvokeOrderChanged();
                }
            }
        }

        protected override void OnWindowFocused(object sender, IntPtr e)
        {
            var index = _windows.IndexOf(e);
            var hasChanged = index == -1 || index >= Settings.NumberOfWindows;
            _windows.Remove(e);
            _windows.Insert(0, e);
            if (hasChanged)
            {
                InvokeOrderChanged();
            }
        }

        protected override void OnWindowOpened(object sender, IntPtr e)
        {
            var windowHandle = e;
            _windows.Insert(0, windowHandle);
            InvokeOrderChanged();
        }
    }
}
