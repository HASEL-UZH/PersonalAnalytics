using System;
using System.Collections.Generic;

namespace WindowRecommender.Models
{
    internal interface IModel
    {
        event EventHandler ScoreChanged;

        string Name { get; }

        Dictionary<IntPtr, double> GetScores();
    }
}
