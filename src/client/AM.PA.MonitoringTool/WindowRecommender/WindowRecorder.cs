using System;
using System.Collections.Generic;
using WindowRecommender.Data;
using WindowRecommender.Native;

namespace WindowRecommender
{
    internal class WindowRecorder
    {
        private readonly Dictionary<IntPtr, string> _processNames;
        private readonly WindowStack _windowStack;

        private Dictionary<IntPtr, double> _scores;
        private List<IntPtr> _ranks;

        internal WindowRecorder(ModelEvents modelEvents, WindowStack windowStack)
        {
            _processNames = new Dictionary<IntPtr, string>();
            _windowStack = windowStack;

            _scores = new Dictionary<IntPtr, double>();
            _ranks = new List<IntPtr>();

            modelEvents.WindowFocused += OnWindowFocused;
            modelEvents.WindowClosed += OnWindowClosed;
        }

        internal void SetScores(Dictionary<IntPtr, double> scores, List<IntPtr> ranks)
        {
            _scores = scores;
            _ranks = ranks;
        }

        private void OnWindowClosed(object sender, IntPtr e)
        {
            var windowHandle = e;
            if (_scores.ContainsKey(windowHandle))
            {
                var score = _scores[windowHandle];
                var rank = _ranks.IndexOf(windowHandle);
                var processName = GetProcessName(windowHandle);
                Queries.SaveEvent(windowHandle, processName, EventName.Close, rank, score);
                _scores.Remove(windowHandle);
                _processNames.Remove(windowHandle);
            }
        }

        private void OnWindowFocused(object sender, IntPtr e)
        {
            var windowHandle = e;
            var processName = GetProcessName(windowHandle);
            if (_scores.ContainsKey(windowHandle))
            {
                var score = _scores[windowHandle];
                var rank = _ranks.IndexOf(windowHandle);
                var zIndex = _windowStack.GetZIndex(windowHandle);
                Queries.SaveEvent(windowHandle, processName, EventName.Focus, rank, score, zIndex);
            }
            else
            {
                Queries.SaveEvent(windowHandle, processName, EventName.Open);
            }
        }

        private string GetProcessName(IntPtr windowHandle)
        {
            if (_processNames.ContainsKey(windowHandle))
            {
                return _processNames[windowHandle];
            }
            var processName = NativeMethods.GetProcessName(windowHandle);
            _processNames[windowHandle] = processName;
            return processName;
        }
    }
}
