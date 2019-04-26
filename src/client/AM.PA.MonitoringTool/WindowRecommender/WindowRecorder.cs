using System;
using System.Collections.Generic;
using System.Linq;
using WindowRecommender.Data;

namespace WindowRecommender
{
    internal class WindowRecorder
    {
        private readonly WindowStack _windowStack;

        private Dictionary<IntPtr, double> _scores;
        private IntPtr[] _ranks;

        internal WindowRecorder(IWindowEvents windowEvents, WindowStack windowStack)
        {
            _windowStack = windowStack;

            _scores = new Dictionary<IntPtr, double>();
            _ranks = new IntPtr[0];

            windowEvents.WindowOpened += OnWindowOpened;
            windowEvents.WindowFocused += OnWindowFocused;
            windowEvents.WindowClosed += OnWindowClosed;
            windowEvents.WindowMinimized += OnWindowMinimized;
        }

        internal void SetScores(Dictionary<IntPtr, double> scores, IEnumerable<IntPtr> ranks)
        {
            _scores = scores;
            _ranks = ranks.ToArray();
        }

        private void OnWindowClosed(object sender, WindowRecord e)
        {
            var windowRecord = e;
            if (_scores.ContainsKey(windowRecord.Handle))
            {
                var score = _scores[windowRecord.Handle];
                var rank = Array.IndexOf(_ranks, windowRecord.Handle);
                Queries.SaveEvent(windowRecord.Handle, windowRecord.ProcessName, EventName.Close, rank, score);
                _scores.Remove(windowRecord.Handle);
            }
        }

        private void OnWindowMinimized(object sender, WindowRecord e)
        {
            var windowRecord = e;
            if (_scores.ContainsKey(windowRecord.Handle))
            {
                var score = _scores[windowRecord.Handle];
                var rank = Array.IndexOf(_ranks, windowRecord.Handle);
                Queries.SaveEvent(windowRecord.Handle, windowRecord.ProcessName, EventName.Minimize, rank, score);
            }
        }

        private void OnWindowFocused(object sender, WindowRecord e)
        {
            var windowRecord = e;
            if (_scores.ContainsKey(windowRecord.Handle))
            {
                var score = _scores[windowRecord.Handle];
                var rank = Array.IndexOf(_ranks, windowRecord.Handle);
                var zIndex = _windowStack.GetZIndex(windowRecord.Handle);
                Queries.SaveEvent(windowRecord.Handle, windowRecord.ProcessName, EventName.Focus, rank, score, zIndex);
            }
            else
            {
                Queries.SaveEvent(windowRecord.Handle, windowRecord.ProcessName, EventName.Open);
            }
        }

        private void OnWindowOpened(object sender, WindowRecord e)
        {
            var windowRecord = e;
            Queries.SaveEvent(windowRecord.Handle, windowRecord.ProcessName, EventName.Open);
        }
    }
}
