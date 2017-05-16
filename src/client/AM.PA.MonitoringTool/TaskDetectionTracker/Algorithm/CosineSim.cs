using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskDetectionTracker.Algorithm
{
    class CosineSim
    {
        private string[] _docs;
        private string[][] _ngramDoc;
        private int _numDocs = 0;
        private int _numTerms = 0;
        private ArrayList _terms;
        private int[][] _termFreq;
        private float[][] _termWeight;
        private int[] _maxTermFreq;
        private int[] _docFreq;


        public class TermVector
        {
            public static float ComputeCosineSimilarity(float[] vector1, float[] vector2)
            {
                if (vector1.Length != vector2.Length)
                    throw new Exception("DIFER LENGTH");


                float denom = (VectorLength(vector1) * VectorLength(vector2));
                if (denom == 0F)
                    return 0F;
                else
                    return (InnerProduct(vector1, vector2) / denom);

            }

            public static float InnerProduct(float[] vector1, float[] vector2)
            {

                if (vector1.Length != vector2.Length)
                    throw new Exception("DIFFER LENGTH ARE NOT ALLOWED");


                float result = 0F;
                for (int i = 0; i < vector1.Length; i++)
                    result += vector1[i] * vector2[i];

                return result;
            }

            public static float VectorLength(float[] vector)
            {
                float sum = 0.0F;
                for (int i = 0; i < vector.Length; i++)
                    sum = sum + (vector[i] * vector[i]);

                return (float)Math.Sqrt(sum);
            }

        }

        private IDictionary _wordsIndex = new Hashtable();

        public void TFIDFMeasure(string[] documents)
        {
            _docs = documents;
            _numDocs = documents.Length;
            MyInit();
        }

        private void GeneratNgramText()
        {

        }

        private ArrayList GenerateTerms(string[] docs)
        {
            ArrayList uniques = new ArrayList();
            _ngramDoc = new string[_numDocs][];
            for (int i = 0; i < docs.Length; i++)
            {
                Tokeniser tokenizer = new Tokeniser();
                string[] words = tokenizer.Partition(docs[i]);

                for (int j = 0; j < words.Length; j++)
                    if (!uniques.Contains(words[j]))
                        uniques.Add(words[j]);

            }
            return uniques;
        }



        private static object AddElement(IDictionary collection, object key, object newValue)
        {
            object element = collection[key];
            collection[key] = newValue;
            return element;
        }

        private int GetTermIndex(string term)
        {
            object index = _wordsIndex[term];
            if (index == null) return -1;
            return (int)index;
        }

        private void MyInit()
        {
            _terms = GenerateTerms(_docs);
            _numTerms = _terms.Count;

            _maxTermFreq = new int[_numDocs];
            _docFreq = new int[_numTerms];
            _termFreq = new int[_numTerms][];
            _termWeight = new float[_numTerms][];

            for (int i = 0; i < _terms.Count; i++)
            {
                _termWeight[i] = new float[_numDocs];
                _termFreq[i] = new int[_numDocs];

                AddElement(_wordsIndex, _terms[i], i);
            }

            GenerateTermFrequency();
            GenerateTermWeight();

        }

        private float Log(float num)
        {
            return (float)Math.Log(num);//log2
        }

        private void GenerateTermFrequency()
        {
            for (int i = 0; i < _numDocs; i++)
            {
                string curDoc = _docs[i];
                IDictionary freq = GetWordFrequency(curDoc);
                IDictionaryEnumerator enums = freq.GetEnumerator();
                _maxTermFreq[i] = int.MinValue;
                while (enums.MoveNext())
                {
                    string word = (string)enums.Key;
                    int wordFreq = (int)enums.Value;
                    int termIndex = GetTermIndex(word);

                    _termFreq[termIndex][i] = wordFreq;
                    _docFreq[termIndex]++;

                    if (wordFreq > _maxTermFreq[i]) _maxTermFreq[i] = wordFreq;
                }
            }
        }


        private void GenerateTermWeight()
        {
            for (int i = 0; i < _numTerms; i++)
            {
                for (int j = 0; j < _numDocs; j++)
                    _termWeight[i][j] = ComputeTermWeight(i, j);
            }
        }

        private float GetTermFrequency(int term, int doc)
        {
            int freq = _termFreq[term][doc];
            int maxfreq = _maxTermFreq[doc];

            return ((float)freq / (float)maxfreq);
        }

        private float GetInverseDocumentFrequency(int term)
        {
            int df = _docFreq[term];
            return Log((float)(_numDocs) / (float)df);
        }

        private float ComputeTermWeight(int term, int doc)
        {
            float tf = GetTermFrequency(term, doc);
            float idf = GetInverseDocumentFrequency(term);
            return tf * idf;
        }

        private float[] GetTermVector(int doc)
        {
            float[] w = new float[_numTerms];
            for (int i = 0; i < _numTerms; i++)
                w[i] = _termWeight[i][doc];


            return w;
        }

        public float GetSimilarity(int doc_i, int doc_j)
        {
            float[] vector1 = GetTermVector(doc_i);
            float[] vector2 = GetTermVector(doc_j);

            return TermVector.ComputeCosineSimilarity(vector1, vector2);

        }

        private IDictionary GetWordFrequency(string input)
        {
            string convertedInput = input.ToLower();

            Tokeniser tokenizer = new Tokeniser();
            String[] words = tokenizer.Partition(convertedInput);
            Array.Sort(words);

            String[] distinctWords = GetDistinctWords(words);

            IDictionary result = new Hashtable();
            for (int i = 0; i < distinctWords.Length; i++)
            {
                object tmp;
                tmp = CountWords(distinctWords[i], words);
                result[distinctWords[i]] = tmp;

            }

            return result;
        }

        private string[] GetDistinctWords(String[] input)
        {
            if (input == null)
                return new string[0];
            else
            {
                ArrayList list = new ArrayList();

                for (int i = 0; i < input.Length; i++)
                    if (!list.Contains(input[i])) // N-GRAM SIMILARITY?				
                        list.Add(input[i]);

                return Tokeniser.ArrayListToArray(list);
            }
        }



        private int CountWords(string word, string[] words)
        {
            int itemIdx = Array.BinarySearch(words, word);

            if (itemIdx > 0)
                while (itemIdx > 0 && words[itemIdx].Equals(word))
                    itemIdx--;

            int count = 0;
            while (itemIdx < words.Length && itemIdx >= 0)
            {
                if (words[itemIdx].Equals(word)) count++;

                itemIdx++;
                if (itemIdx < words.Length)
                    if (!words[itemIdx].Equals(word)) break;

            }

            return count;
        }
    }
}
