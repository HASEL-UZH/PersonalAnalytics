using System;
using System.Collections.Generic;

namespace WindowRecommender.Models
{
    internal interface IModel
    {
        event EventHandler ScoreChanged;

        Dictionary<IntPtr, double> GetScores();
    }
}
