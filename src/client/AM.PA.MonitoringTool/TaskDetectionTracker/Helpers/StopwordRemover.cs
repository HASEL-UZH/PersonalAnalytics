// Created by Katja Kevic (kevic@ifi.uzh.ch) from the University of Zurich
// Created: 2017-05-16
// 
// Licensed under the MIT License.

using System.Collections.Generic;
using TaskDetectionTracker.Properties;

namespace TaskDetectionTracker.Helpers
{
    internal class StopwordRemover
    {
        public static ISet<string> englishStopwords;
        //private const string fileName = "stopwords.txt";

        public StopwordRemover()
        {
            if (englishStopwords == null)
            {
                englishStopwords = new HashSet<string>();
                var arr = Resources.stopwords.Split(new char[] { '\n', '\r' });
                englishStopwords.UnionWith(arr);
            }
        }

        public bool IsStopword(string s)
        {
            if (englishStopwords.Contains(s.ToLower())) return true;
            return false;
        }
    }
}
