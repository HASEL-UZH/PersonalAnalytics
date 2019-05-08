using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowRecommender.Models
{
    internal class ModelCore
    {
        internal const string MergedScoreName = "Merged";

        internal event EventHandler<Dictionary<IntPtr, Dictionary<string, double>>> ScoreChanged;
        internal event EventHandler<List<IntPtr>> WindowsChanged;

        private readonly (IModel model, double weight)[] _models;
        private readonly double _weightSum;

        private List<KeyValuePair<IntPtr, double>> _orderedWindows;
        private List<IntPtr> _topWindows;

        internal ModelCore((IModel model, double weight)[] models)
        {
            _orderedWindows = new List<KeyValuePair<IntPtr, double>>();
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

        private Dictionary<IntPtr, Dictionary<string, double>> GetScores()
        {
            var scores = new Dictionary<IntPtr, Dictionary<string, double>>();
            foreach (var (model, weight) in _models)
            {
                var relativeWeight = weight / _weightSum;
                var normalizedModelScores = NormalizeScores(model.GetScores());
                foreach (var score in normalizedModelScores)
                {
                    if (!scores.ContainsKey(score.Key))
                    {
                        scores.Add(score.Key, new Dictionary<string, double>());
                    }

                    if (!scores[score.Key].ContainsKey(MergedScoreName))
                    {
                        scores[score.Key].Add(MergedScoreName, 0);
                    }

                    scores[score.Key][MergedScoreName] += score.Value * relativeWeight;
                    scores[score.Key][model.Name] = score.Value;
                }
            }
            return scores;
        }

        private void InvokeEvents(object origin)
        {
            var scores = GetScores();
            var mergedScores = scores.ToDictionary(pair => pair.Key, pair => pair.Value[MergedScoreName]);
            var orderedWindows = mergedScores.OrderByDescending(x => x.Value).ToList();
            if (!_orderedWindows.SequenceEqual(orderedWindows))
            {
                _orderedWindows = orderedWindows;
                ScoreChanged?.Invoke(origin, scores);
                var topWindows = orderedWindows.Select(x => x.Key).Take(Settings.NumberOfWindows).ToList();
                if (!_topWindows.SequenceEqual(topWindows))
                {
                    _topWindows = topWindows;
                    WindowsChanged?.Invoke(this, _topWindows);
                }
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
