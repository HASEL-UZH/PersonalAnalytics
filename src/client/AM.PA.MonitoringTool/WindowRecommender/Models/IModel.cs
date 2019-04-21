using System;
using System.Collections.Generic;

namespace WindowRecommender.Models
{
    internal interface IModel
    {
        event EventHandler OrderChanged;

        Dictionary<IntPtr, double> GetScores();

        /// <summary>
        /// Set the initial list of windows on startup.
        /// Does not trigger an <see cref="OrderChanged"/>-event.
        /// </summary>
        /// <param name="windows">Currently open windows in z-index order.</param>
        void SetWindows(List<IntPtr> windows);
    }
}
