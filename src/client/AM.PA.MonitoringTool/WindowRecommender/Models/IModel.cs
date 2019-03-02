using System;
using System.Collections.Generic;

namespace WindowRecommender.Models
{
    internal interface IModel
    {
        event EventHandler OrderChanged;

        Dictionary<IntPtr, double> GetScores();
        void SetWindows(List<IntPtr> windows);
    }
}
