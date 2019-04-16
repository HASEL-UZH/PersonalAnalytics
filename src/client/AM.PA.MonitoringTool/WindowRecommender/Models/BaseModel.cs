using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowRecommender.Models
{
    internal abstract class BaseModel : IModel
    {
        public event EventHandler OrderChanged;

        internal BaseModel(ModelEvents modelEvents)
        {
            modelEvents.WindowOpened += OnWindowOpened;
            modelEvents.WindowFocused += OnWindowFocused;
            modelEvents.WindowClosed += OnWindowClosed;
        }

        public abstract Dictionary<IntPtr, double> GetScores();

        public abstract void SetWindows(List<IntPtr> windows);

        protected abstract void OnWindowClosed(object sender, IntPtr e);

        protected abstract void OnWindowFocused(object sender, IntPtr e);

        protected abstract void OnWindowOpened(object sender, IntPtr e);

        protected virtual void InvokeOrderChanged()
        {
            OrderChanged?.Invoke(this, null);
        }
        internal static List<IntPtr> GetTopWindows(Dictionary<IntPtr, double> scores)
        {
            return Utils.GetTopEntries(scores, Settings.NumberOfWindows);
        }
    }
}
