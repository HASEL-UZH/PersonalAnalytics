using System;
using System.Collections;
using System.Collections.Generic;

namespace WindowRecommenderTests.Tools
{
    internal class ScoreComparer : IComparer
    {
        /// <summary>
        /// Delta for comparing score double values
        /// </summary>
        private const double Delta = 0.000001;

        public int Compare(object x, object y)
        {
            KeyValuePair<IntPtr, double> scoreX;
            KeyValuePair<IntPtr, double> scoreY;
            if (x == null || y == null)
            {
                throw new ArgumentException("Cannot compare null scores.");
            }
            try
            {
                scoreX = (KeyValuePair<IntPtr, double>)x;
                scoreY = (KeyValuePair<IntPtr, double>)y;
            }
            catch (InvalidCastException e)
            {
                throw new ArgumentException("Given values are not valid scores.", e);
            }

            if (scoreX.Key != scoreY.Key)
            {
                return scoreX.Key.ToInt32() - scoreY.Key.ToInt32();
            }

            var scoreDifference = scoreX.Value - scoreY.Value;
            if (Math.Abs(scoreDifference) > Delta)
            {
                return scoreDifference > 0 ? 1 : -1;
            }

            return 0;
        }
    }
}
