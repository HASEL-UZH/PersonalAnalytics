using System;
using System.Collections.Generic;
using WindowRecommender.Util;

namespace WindowRecommender.Models
{
    internal abstract class BaseModel : IModel
    {
        public event EventHandler ScoreChanged;

        internal BaseModel(IWindowEvents windowEvents)
        {
            windowEvents.Setup += (sender, e) => Setup(e);
        }

        public abstract Dictionary<IntPtr, double> GetScores();

        protected abstract void Setup(List<WindowRecord> windowRecords);

        protected void InvokeScoreChanged()
        {
            ScoreChanged?.Invoke(this, null);
        }
    }
}
