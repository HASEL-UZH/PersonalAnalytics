using System;
using System.Collections.Generic;
using System.Linq;
using WindowRecommender.Util;

namespace WindowRecommender.Models
{
    internal class ModelCore
    {
        internal event EventHandler<Dictionary<IntPtr, double>> ScoreChanged;
        internal event EventHandler<List<IntPtr>> WindowsChanged;

        private readonly (IModel model, double weight)[] _models;
        private readonly double _weightSum;

        private List<IntPtr> _topWindows;

        internal ModelCore((IModel model, double weight)[] models)
        {
            _topWindows = new List<IntPtr>();
            _models = models;
            _weightSum = _models.Sum(modelWeight => modelWeight.weight);
            foreach (var (model, _) in _models)
            {
                model.ScoreChanged += OnScoreChanged;
            }
        }

        internal void Start()
        {
            InvokeEvents(this);
        }

        internal List<IntPtr> GetTopWindows()
        {
            return _topWindows;
        }

        private Dictionary<IntPtr, double> GetScores()
        {
            var mergedScores = new Dictionary<IntPtr, double>();
            foreach (var (model, weight) in _models)
            {
                var relativeWeight = weight / _weightSum;
                var normalizedModelScores = NormalizeScores(model.GetScores());
                foreach (var score in normalizedModelScores)
                {
                    if (!mergedScores.ContainsKey(score.Key))
                    {
                        mergedScores.Add(score.Key, 0);
                    }
                    mergedScores[score.Key] += score.Value * relativeWeight;
                }
            }
            return mergedScores;
        }

        private void InvokeEvents(object origin)
        {
            var mergedScores = GetScores();
            ScoreChanged?.Invoke(origin, mergedScores);
            var newTopWindows = Utils.GetTopEntries(mergedScores, Settings.NumberOfWindows).ToList();
            if (!_topWindows.SequenceEqual(newTopWindows))
            {
                _topWindows = newTopWindows;
                WindowsChanged?.Invoke(this, _topWindows);
            }
        }

        private void OnScoreChanged(object sender, EventArgs e)
        {
            InvokeEvents(sender);
        }

        internal static Dictionary<IntPtr, double> NormalizeScores(Dictionary<IntPtr, double> scores)
        {
            var scoreSum = scores.Sum(pair => pair.Value);
            if (double.IsNaN(0 / scoreSum)) // No need to normalize if all scores are 0
            {
                return scores;
            }
            return scores.ToDictionary(pair => pair.Key, pair => pair.Value / scoreSum);
        }
    }
}
