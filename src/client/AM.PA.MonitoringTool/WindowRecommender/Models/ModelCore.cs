using System;
using System.Collections.Generic;
using System.Linq;
using WindowRecommender.Native;

namespace WindowRecommender.Models
{
    internal class ModelCore
    {
        internal event EventHandler<Dictionary<IntPtr, double>> ScoreChanged;

        private readonly (IModel model, double weight)[] _models;
        private readonly double _weightSum;

        internal ModelCore((IModel model, double weight)[] models)
        {
            _models = models;
            _weightSum = _models.Sum((modelWeight) => modelWeight.weight);
            foreach (var (model, _) in _models)
            {
                model.OrderChanged += OnOrderChanged;
            }
        }

        internal void Start()
        {
            var windows = NativeMethods.GetOpenWindows();
            foreach (var (model, _) in _models)
            {
                model.SetWindows(windows);
            }
            MergeScores();
        }

        internal Dictionary<IntPtr, double> GetScores()
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

        private void OnOrderChanged(object sender, EventArgs e)
        {
            MergeScores();
        }

        private void MergeScores()
        {
            var mergedScores = GetScores();
            ScoreChanged?.Invoke(this, mergedScores);
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
