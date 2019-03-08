using System;
using System.Collections.Generic;
using System.Linq;
using WindowRecommender.Native;

namespace WindowRecommender.Models
{
    internal class ModelCore
    {
        internal event EventHandler<Dictionary<IntPtr, double>> ScoreChanged;

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
            MergeScores();
        }

        private void OnOrderChanged(object sender, EventArgs e)
        {
            MergeScores();
        }

        private void MergeScores()
        {
            var mergedScores = new Dictionary<IntPtr, double>();
            foreach (var model in _models)
            {
                var normalizedModelScores = NormalizeScores(model.Key.GetScores());
                foreach (var score in normalizedModelScores)
                {
                    if (!mergedScores.ContainsKey(score.Key))
                    {
                        mergedScores.Add(score.Key, 0);
                    }
                    mergedScores[score.Key] += score.Value * model.Value;
                }
            }
            ScoreChanged?.Invoke(this, mergedScores);
        }

        internal static List<IntPtr> GetTopWindows(Dictionary<IntPtr, double> scores)
        {
            return scores.OrderByDescending(x => x.Value).Select(x => x.Key).Take(Settings.NumberOfWindows).ToList();
        }

        internal static Dictionary<IntPtr, double> NormalizeScores(Dictionary<IntPtr, double> scores)
        {
            var scoreSum = scores.Sum(pair => pair.Value);
            if (double.IsNaN(0 / scoreSum)) // scoreSum == 0
            {
                return scores;
            }
            return scores.ToDictionary(pair => pair.Key, pair => pair.Value / scoreSum);
        }
    }
}
