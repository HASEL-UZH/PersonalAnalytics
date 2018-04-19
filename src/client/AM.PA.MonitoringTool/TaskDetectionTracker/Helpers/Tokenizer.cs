// Created by Katja Kevic (kevic@ifi.uzh.ch) from the University of Zurich
// Created: 2017-05-16
// 
// Licensed under the MIT License.

using TaskDetectionTracker.Helpers;
using System.Collections;
using System.Text.RegularExpressions;

namespace TaskDetectionTracker.Helpers
{
    /// <summary>
    /// Partitions text into tokens.
    /// Source: https://www.codeproject.com/Articles/12098/Term-frequency-Inverse-document-frequency-implemen
    /// Author: Thanh Dao, modified by Katja Kevic, 2017-05-16
    /// </summary>
    internal class Tokenizer
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
            input = input.ToLower(System.Globalization.CultureInfo.InvariantCulture);

            string[] tokens = r.Split(input);

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

        public Tokenizer()
        {
        }
    }
}
