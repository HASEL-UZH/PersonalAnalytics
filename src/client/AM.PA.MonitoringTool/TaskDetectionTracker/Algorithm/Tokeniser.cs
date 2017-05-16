using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TaskDetectionTracker.Algorithm
{
    /// <summary>
    /// Partitions text into tokens.
    /// Source: https://www.codeproject.com/Articles/12098/Term-frequency-Inverse-document-frequency-implemen
    /// Author: Thanh Dao, modified by Katja Kevic, 2017-05-16
    /// </summary>
    class Tokeniser
    {
        private StopwordRemover sw = new StopwordRemover();

        public static string[] ArrayListToArray(ArrayList arraylist)
        {
            string[] array = new string[arraylist.Count];
            for (int i = 0; i < arraylist.Count; i++) array[i] = (string)arraylist[i];
            return array;
        }

        public string[] Partition(string input)
        {
            Regex r = new Regex("([ \\t{}():;. \n/\\-\\&\\*\\\\_@\\?\\[\\]])");
            input = input.ToLower();

            String[] tokens = r.Split(input);

            ArrayList filter = new ArrayList();

            for (int i = 0; i < tokens.Length; i++)
            {
                MatchCollection mc = r.Matches(tokens[i]);
                if (mc.Count <= 0 && tokens[i].Trim().Length > 1
                    && !sw.IsStopword(tokens[i]))
                {
                    filter.Add(tokens[i]);
                }
            }

            return ArrayListToArray(filter);
        }


        public Tokeniser()
        {
        }
    }
}
