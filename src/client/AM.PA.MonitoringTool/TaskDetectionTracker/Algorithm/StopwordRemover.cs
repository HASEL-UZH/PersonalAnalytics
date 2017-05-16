using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskDetectionTracker.Properties;

namespace TaskDetectionTracker.Algorithm
{
    class StopwordRemover
    {
        public static ISet<String> englishStopwords;
        private const String fileName = "stopwords.txt";

        public StopwordRemover()
        {
            if (englishStopwords == null)
            {
                englishStopwords = new HashSet<string>();
                String[] arr = Resources.stopwords.Split(new char[] { '\n', '\r' });
                englishStopwords.UnionWith(arr);
            }
        }

        public bool IsStopword(String s)
        {
            if (englishStopwords.Contains(s.ToLower())) return true;
            return false;
        }
    }
}
