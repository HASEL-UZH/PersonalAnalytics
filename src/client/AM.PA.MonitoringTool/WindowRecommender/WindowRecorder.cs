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

            windowEvents.Setup += OnSetup;
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

        private void OnSetup(object sender, List<WindowRecord> e)
        {
            var windowRecords = e;
            Queries.SaveEvents(EventName.Initial, windowRecords.Select(GetBasicEntry));
        }

        private void OnWindowClosed(object sender, WindowRecord e)
        {
            var windowRecord = e;
            if (_scores.ContainsKey(windowRecord.Handle))
            {
                Queries.SaveEvent(EventName.Close, GetExtendedEntry(windowRecord));
                _scores.Remove(windowRecord.Handle);
            }
        }

        private void OnWindowMinimized(object sender, WindowRecord e)
        {
            var windowRecord = e;
            if (_scores.ContainsKey(windowRecord.Handle))
            {
                Queries.SaveEvent(EventName.Minimize, GetExtendedEntry(windowRecord));
            }
        }

        private void OnWindowFocused(object sender, WindowRecord e)
        {
            var windowRecord = e;
            if (_scores.ContainsKey(windowRecord.Handle))
            {
                Queries.SaveEvent(EventName.Focus, GetExtendedEntry(windowRecord));
            }
            else
            {
                Queries.SaveEvent(EventName.Open, GetBasicEntry(windowRecord));
            }
        }

        private void OnWindowOpened(object sender, WindowRecord e)
        {
            var windowRecord = e;
            Queries.SaveEvent(EventName.Open, GetBasicEntry(windowRecord));
        }

        private DatabaseEntry GetBasicEntry(WindowRecord windowRecord)
        {
            var zIndex = _windowStack.GetZIndex(windowRecord);
            return new DatabaseEntry(windowRecord.Handle, windowRecord.Title, windowRecord.ProcessName, zIndex);
        }

        private DatabaseEntry GetExtendedEntry(WindowRecord windowRecord)
        {
            var zIndex = _windowStack.GetZIndex(windowRecord);
            var rank = Array.IndexOf(_ranks, windowRecord.Handle);
            var score = _scores[windowRecord.Handle];
            return new DatabaseEntry(windowRecord.Handle, windowRecord.Title, windowRecord.ProcessName, zIndex, rank, score);
        }
    }
}
