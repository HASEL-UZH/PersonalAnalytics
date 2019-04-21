using System;
using System.Collections.Generic;
using System.Linq;
using Accord.MachineLearning;
using WindowRecommender.Native;

namespace WindowRecommender.Models
{
    internal class TitleSimilarity : BaseModel
    {
        private Dictionary<IntPtr, string[]> _titles;
        private Dictionary<IntPtr, double> _scores;
        private IntPtr[] _topWindows;
        private IntPtr _currentWindow;

        public TitleSimilarity(ModelEvents modelEvents) : base(modelEvents)
        {
            modelEvents.WindowRenamed += OnWindowRenamed;
            _scores = new Dictionary<IntPtr, double>();
            _topWindows = new IntPtr[0];
            _titles = new Dictionary<IntPtr, string[]>();
        }

        public override Dictionary<IntPtr, double> GetScores()
        {
            return _scores;
        }

        public override void SetWindows(List<IntPtr> windows)
        {
            if (windows.Count > 0)
            {
                _currentWindow = windows.First();
                _titles = windows
                    .Select(windowHandle => (windowHandle, preparedTitle: GetPreparedWindowTitle(windowHandle)))
                    .Where(tuple => tuple.preparedTitle.Length != 0)
                    .ToDictionary(tuple => tuple.windowHandle, tuple => tuple.preparedTitle);
                _scores = CalculateScores();
                _topWindows = GetTopWindows(_scores).ToArray();
            }
        }

        protected override void OnWindowClosed(object sender, IntPtr e)
        {
            var windowHandle = e;
            _scores.Remove(windowHandle);
            _titles.Remove(windowHandle);
        }

        protected override void OnWindowFocused(object sender, IntPtr e)
        {
            var windowHandle = e;
            _currentWindow = windowHandle;
            CalculateScoreChanges();
        }

        protected override void OnWindowOpened(object sender, IntPtr e)
        {
            var windowHandle = e;
            _currentWindow = windowHandle;
            var preparedTitle = GetPreparedWindowTitle(windowHandle);
            if (preparedTitle.Length != 0)
            {
                _titles[windowHandle] = preparedTitle;
            }
            CalculateScoreChanges();
        }

        private void OnWindowRenamed(object sender, IntPtr e)
        {
            var windowHandle = e;
            var preparedTitle = GetPreparedWindowTitle(windowHandle);
            if (!_titles.ContainsKey(windowHandle) || !_titles[windowHandle].SequenceEqual(preparedTitle))
            {
                if (preparedTitle.Length != 0)
                {
                    _titles[windowHandle] = preparedTitle;
                }
                else
                {
                    _titles.Remove(windowHandle);
                }
                CalculateScoreChanges();
            }
        }

        private void CalculateScoreChanges()
        {
            _scores = CalculateScores();
            var newTop = GetTopWindows(_scores).ToArray();
            if (!_topWindows.SequenceEqual(newTop))
            {
                InvokeOrderChanged();
                _topWindows = newTop;
            }
            InvokeOrderChanged();
        }

        private Dictionary<IntPtr, double> CalculateScores()
        {
            if (!_titles.ContainsKey(_currentWindow))
            {
                return new Dictionary<IntPtr, double>();
            }
            var windowTitles = _titles.Where(pair => pair.Key != _currentWindow).ToArray();
            var titles = windowTitles
                .Select(pair => pair.Value)
                .Concat(new[] { new string[0], _titles[_currentWindow] }).ToArray();
            var vectors = new TFIDF().Learn(titles).Transform(titles);
            var currentWindowVector = vectors.Last();
            var scores = vectors
                .Take(windowTitles.Length)
                .Select(vector => Utils.CosineSimilarity(vector, currentWindowVector))
                .Select((similarity, i) => (similarity, windowHandle: windowTitles[i].Key))
                .Where(tuple => tuple.similarity > 0)
                .ToDictionary(tuple => tuple.windowHandle, tuple => tuple.similarity);
            return scores;
        }

        private static string[] GetPreparedWindowTitle(IntPtr windowHandle)
        {
            return TextUtils.PrepareTitle(NativeMethods.GetWindowTitle(windowHandle)).ToArray();
        }
    }
}
