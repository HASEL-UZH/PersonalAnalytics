using System;
using System.Collections.Generic;
using System.Linq;
using WindowRecommender.Native;

namespace WindowRecommender
{
    internal class ModelCore
    {
        internal event EventHandler<IEnumerable<Rectangle>> WindowsHaze;

        private readonly Dictionary<IModel, int> _models;

        internal ModelCore(Dictionary<IModel, int> modelWeights)
        {
            _models = modelWeights;
            foreach (var model in _models.Keys)
            {
                model.OrderChanged += OnOrderChanged;
            }
        }

        internal void Start()
        {
            var windows = NativeMethods.GetOpenWindows();
            foreach (var model in _models.Keys)
            {
                model.SetWindows(windows);
            }
            HazeWindows();
        }

        private void OnOrderChanged(object sender, EventArgs e)
        {
            HazeWindows();
        }

        private void HazeWindows()
        {
            var scores = new Dictionary<IntPtr, double>();
            foreach (var model in _models)
            {
                foreach (var score in model.Key.GetScores())
                {
                    if (!scores.ContainsKey(score.Key))
                    {
                        scores.Add(score.Key, 0);
                    }
                    scores[score.Key] += score.Value * model.Value;
                }
            }
            foreach (var windowHandle in scores.Keys.ToList())
            {
                scores[windowHandle] /= _models.Count;
            }
            var topWindows = scores.OrderByDescending(x => x.Value).Select(x => x.Key).Take(Settings.NumberOfWindows);
            var windowRects = topWindows.Select(windowHandle => (Rectangle)NativeMethods.GetWindowRectangle(windowHandle));
            WindowsHaze?.Invoke(this, windowRects);
        }
    }
}
