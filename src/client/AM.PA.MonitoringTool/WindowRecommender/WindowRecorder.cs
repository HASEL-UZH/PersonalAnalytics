using System;
using System.Collections.Generic;
using System.Linq;
using WindowRecommender.Data;
using WindowRecommender.Native;

namespace WindowRecommender
{
    internal class WindowRecorder
    {
        private readonly Dictionary<IntPtr, string> _processNames;
        private readonly WindowStack _windowStack;

        private Dictionary<IntPtr, double> _scores;
        private IntPtr[] _ranks;

        internal WindowRecorder(ModelEvents modelEvents, WindowStack windowStack)
        {
            _processNames = new Dictionary<IntPtr, string>();
            _windowStack = windowStack;

            _scores = new Dictionary<IntPtr, double>();
            _ranks = new IntPtr[0];

            modelEvents.WindowOpened += OnWindowOpened;
            modelEvents.WindowFocused += OnWindowFocused;
            modelEvents.WindowClosed += OnWindowClosed;
        }

        internal void SetScores(Dictionary<IntPtr, double> scores, IEnumerable<IntPtr> ranks)
        {
            _scores = scores;
            _ranks = ranks.ToArray();
        }

        private void OnWindowClosed(object sender, IntPtr e)
        {
            var windowHandle = e;
            if (_scores.ContainsKey(windowHandle))
            {
                var score = _scores[windowHandle];
                var rank = Array.IndexOf(_ranks, windowHandle);
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
                var rank = Array.IndexOf(_ranks, windowHandle);
                var zIndex = _windowStack.GetZIndex(windowHandle);
                Queries.SaveEvent(windowHandle, processName, EventName.Focus, rank, score, zIndex);
            }
            else
            {
                Queries.SaveEvent(windowHandle, processName, EventName.Open);
            }
        }

        private void OnWindowOpened(object sender, IntPtr e)
        {
            var windowHandle = e;
            var processName = GetProcessName(windowHandle);
            Queries.SaveEvent(windowHandle, processName, EventName.Open);
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
