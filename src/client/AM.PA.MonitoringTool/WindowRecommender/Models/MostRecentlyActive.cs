using System;
using System.Collections.Generic;

namespace WindowRecommender.Models
{
    internal class MostRecentlyActive : IModel
    {
        public event EventHandler OrderChanged;

        private List<IntPtr> _windows;

        internal MostRecentlyActive(ModelEvents modelEvents)
        {
            _windows = new List<IntPtr>();
            modelEvents.WindowFocused += OnWindowFocused;
            modelEvents.WindowClosed += OnWindowClosed;
        }

        public Dictionary<IntPtr, double> GetScores()
        {
            var scores = new Dictionary<IntPtr, double>();
            for (var i = 0; i < _windows.Count; i++)
            {
                scores[_windows[i]] = i < Settings.NumberOfWindows ? 1 : 0;
            }

            return scores;
        }

        public void SetWindows(List<IntPtr> windows)
        {
            _windows = windows;
        }

        private void OnWindowClosed(object sender, IntPtr e)
        {
            var index = _windows.IndexOf(e);
            if (index != -1)
            {
                var hasChanged = index < Settings.NumberOfWindows;
                _windows.Remove(e);
                if (hasChanged)
                {
                    OrderChanged?.Invoke(this, null);
                }
            }
        }

        private void OnWindowFocused(object sender, IntPtr e)
        {
            var index = _windows.IndexOf(e);
            var hasChanged = index == -1 || index >= Settings.NumberOfWindows;
            _windows.Remove(e);
            _windows.Insert(0, e);
            if (hasChanged)
            {
                OrderChanged?.Invoke(this, null);
            }
        }
    }
}
