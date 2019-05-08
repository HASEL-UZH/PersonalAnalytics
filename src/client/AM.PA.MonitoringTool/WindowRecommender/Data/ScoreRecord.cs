using System;

namespace WindowRecommender.Data
{
    internal struct ScoreRecord
    {
        internal readonly string WindowHandle;
        internal readonly string ModelName;
        internal readonly double Score;

        public ScoreRecord(IntPtr windowHandle, string modelName, double score)
        {
            WindowHandle = windowHandle.ToString();
            ModelName = modelName;
            Score = score;
        }
    }
}