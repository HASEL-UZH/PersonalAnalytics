using System;
using System.Collections.Generic;
using WindowRecommender.Util;

namespace WindowRecommender.Models
{
    internal abstract class BaseModel : IModel
    {
        public event EventHandler OrderChanged;

        internal BaseModel(IWindowEvents windowEvents)
        {
            windowEvents.Setup += (sender, e) => Setup(e);
        }

        public abstract Dictionary<IntPtr, double> GetScores();

        protected abstract void Setup(List<WindowRecord> windowRecords);

        protected void InvokeOrderChanged()
        {
            OrderChanged?.Invoke(this, null);
        }

        internal static IEnumerable<IntPtr> GetTopWindows(Dictionary<IntPtr, double> scores)
        {
            return Utils.GetTopEntries(scores, Settings.NumberOfWindows);
        }
    }
}
