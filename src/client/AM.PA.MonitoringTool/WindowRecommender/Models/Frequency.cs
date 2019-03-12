using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace WindowRecommender.Models
{
    internal class Frequency : IModel
    {
        private readonly List<(IntPtr windowHandle, DateTime dateTime)> _focusEvents;
        private readonly HashSet<IntPtr> _closedWindows;

        private Dictionary<IntPtr, double> _scores;
        private List<IntPtr> _topWindows;

        public event EventHandler OrderChanged;

        internal Frequency(ModelEvents modelEvents)
        {
            _scores = new Dictionary<IntPtr, double>();
            _focusEvents = new List<(IntPtr windowHandle, DateTime dateTime)>();
            _closedWindows = new HashSet<IntPtr>();
            _topWindows = new List<IntPtr>();

            var timer = new Timer(Settings.DurationIntervalSeconds * 1000)
            {
                AutoReset = true,
                Enabled = true
            };
            timer.Elapsed += OnInterval;

            modelEvents.WindowFocused += OnWindowFocused;
            modelEvents.WindowClosed += OnWindowClosed;
        }

        internal void OnInterval(object sender, ElapsedEventArgs e)
        {
            var cutoff = DateTime.Now.AddMinutes(-Settings.DurationTimeframeMinutes);

            // Remove outdated events and events of closed windows
            _focusEvents.RemoveAll(tuple => _closedWindows.Contains(tuple.windowHandle) || tuple.dateTime < cutoff);

            // Group events by window, count events, and divide by total count
            var eventCount = (double)_focusEvents.Count;
            _scores = _focusEvents.GroupBy(tuple => tuple.windowHandle)
                .ToDictionary(grouping => grouping.Key, grouping => grouping.Count() / eventCount);

            _closedWindows.Clear();

            var newTop = ModelCore.GetTopWindows(_scores);
            if (!_topWindows.SequenceEqual(newTop))
            {
                OrderChanged?.Invoke(this, null);
                _topWindows = newTop;
            }
        }

        public Dictionary<IntPtr, double> GetScores()
        {
            return _scores;
        }

        public void SetWindows(List<IntPtr> windows)
        {
            _focusEvents.Add((windowHandle: windows[0], dateTime: DateTime.Now));
        }

        private void OnWindowClosed(object sender, IntPtr e)
        {
            var windowHandle = e;
            _closedWindows.Add(windowHandle);
        }

        private void OnWindowFocused(object sender, IntPtr e)
        {
            var windowHandle = e;
            _focusEvents.Add((windowHandle: windowHandle, dateTime: DateTime.Now));
        }
    }
}
