using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using WindowRecommender.Util;

namespace WindowRecommender.Models
{
    internal class Duration : BaseModel
    {
        private readonly Dictionary<IntPtr, double> _scores;
        private readonly List<(IntPtr windowHandle, DateTime dateTime)> _focusEvents;
        private readonly HashSet<IntPtr> _closedWindows;

        private IntPtr[] _topWindows;

        internal Duration(IWindowEvents windowEvents) : base(windowEvents)
        {
            _scores = new Dictionary<IntPtr, double>();
            _focusEvents = new List<(IntPtr windowHandle, DateTime dateTime)>();
            _closedWindows = new HashSet<IntPtr>();
            _topWindows = new IntPtr[0];

            windowEvents.WindowOpenedOrFocused += OnWindowOpenedOrFocused;
            windowEvents.WindowClosedOrMinimized += OnWindowClosedOrMinimized;

            var timer = new Timer(Settings.DurationIntervalSeconds * 1000)
            {
                AutoReset = true,
                Enabled = true
            };
            timer.Elapsed += OnInterval;
        }

        internal void OnInterval(object sender, ElapsedEventArgs e)
        {
            var cutoff = DateTime.Now.AddMinutes(-Settings.DurationTimeframeMinutes);
            var lastPoll = DateTime.Now.AddSeconds(-Settings.DurationIntervalSeconds);

            // Add dummy event to have and end for last focus and create pairs
            var currentEvents = _focusEvents.Concat(new[]
            {
                (windowHandle: IntPtr.Zero, dateTime: DateTime.Now)
            }).Pairwise(Tuple.Create).ToList();

            foreach (var ((windowHandle, startTime), (_, endTime)) in currentEvents)
            {
                var score = (endTime - startTime).TotalMinutes / Settings.DurationTimeframeMinutes;

                // Duration outside relevant timeframe -> Remove
                if (endTime <= cutoff)
                {
                    _focusEvents.RemoveAt(0);
                    score = -score;
                }
                // Duration starts outside relevant timeframe -> Reduce
                else if (startTime < cutoff)
                {
                    _focusEvents[0] = (windowHandle, dateTime: cutoff);
                    score = -(cutoff - startTime).TotalMinutes / Settings.DurationTimeframeMinutes;
                }
                // Duration extends into new timeframe
                else if (endTime > lastPoll)
                {
                    // Duration started before last poll -> Increase
                    if (startTime < lastPoll)
                    {
                        score = (endTime - lastPoll).TotalMinutes / Settings.DurationTimeframeMinutes;
                    }
                    // Duration started after last pool -> Add
                    else
                    {
                        if (!_scores.ContainsKey(windowHandle) && !_closedWindows.Contains(windowHandle))
                        {
                            _scores[windowHandle] = 0;
                        }
                    }
                }
                // Skip events that do not reach the borders as their scores don't change
                else
                {
                    continue;
                }

                // Update score if it was added or not yet deleted
                if (_scores.ContainsKey(windowHandle))
                {
                    _scores[windowHandle] += score;
                    // Remove entries when score is 0 (or close enough for floating point values)
                    if (Math.Abs(_scores[windowHandle]) < 0.0000001)
                    {
                        _scores.Remove(windowHandle);
                    }
                }
            }
            _closedWindows.Clear();

            var newTop = GetTopWindows(_scores).ToArray();
            if (!_topWindows.SequenceEqual(newTop))
            {
                InvokeOrderChanged();
                _topWindows = newTop;
            }
        }

        public override Dictionary<IntPtr, double> GetScores()
        {
            return _scores;
        }

        protected override void Setup(List<WindowRecord> windowRecords)
        {
            if (windowRecords.Count > 0)
            {
                var windowHandle = windowRecords.First().Handle;
                _focusEvents.Add((windowHandle, dateTime: DateTime.Now));
                _topWindows = new[] { windowHandle };
            }
        }

        private void OnWindowClosedOrMinimized(object sender, WindowRecord e)
        {
            var windowRecord = e;
            if (_scores.ContainsKey(windowRecord.Handle))
            {
                _scores.Remove(windowRecord.Handle);
            }
            else
            {
                _closedWindows.Add(windowRecord.Handle);
            }
        }

        private void OnWindowOpenedOrFocused(object sender, WindowRecord e)
        {
            var windowRecord = e;
            _focusEvents.Add((windowHandle: windowRecord.Handle, dateTime: DateTime.Now));
        }
    }
}
