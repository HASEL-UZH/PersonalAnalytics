// Created by Rohit Kaushik (f20150115@goa.bits-pilani.ac.in) at the University of Zurich
// Created: 2018-07-13
// 
// Licensed under the MIT License.

using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

namespace SlackTracker.Analysis
{
    internal class TextRank
    {

        #region Keyword Extraction

        ///perform Text Rank algo on doc and retrieves keywords
        ///returns an empty list if no keywords are found
        public static List<string> GetKeywords(string doc, int n = 10)
        {
            List<string> keywords = new List<string>();

            if (string.IsNullOrEmpty(doc)) {return keywords;}

            try
            {
                List<string> sentences = Helpers.SentenceSplitter(doc.ToLower());
                sentences = FilterSentences(sentences);

                if(!sentences.Any()) {return keywords;}

                List<string> words = Helpers.Tokenize(sentences);
                List<string> tags = Helpers.GetPosTags(words);

                if (words.Count == 0) {return keywords;}

                List<CandidateWord> CandidateWords = FilterWords(words, tags);

                if (CandidateWords.Count == 0) {return keywords;}

                keywords.AddRange(RankWords(words, CandidateWords));
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return keywords;
        }

        /// <summary>
        /// Pre-processing step for TextRank algorithm.
        /// Include words only with certain POS tag
        /// </summary>
        /// <param name="words"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        private static List<CandidateWord> FilterWords(List<string> words, List<string> tags)
        {
            //List of POS tags to be included in candidate keywords
            //List<string> include = new List<string>() { "JJ", "NN", "NNP", "NNS", "VB", "VBN", "VBG"};

            List<string> include = new List<string>() { "JJ", "NN", "NNP"};
            Dictionary<string, CandidateWord> wordDict = new Dictionary<string, CandidateWord>();
            List<string> smileyList = File.ReadLines(AppDomain.CurrentDomain.BaseDirectory + @"../../../SlackTracker/Analysis/resources/wordnet/dict/smileys.txt").ToList();
            int word_count = words.Count;
            int tag_count = tags.Count;

            if (word_count != tag_count)
            {
                throw new Exception("words and tags list do not match");
            }

            for (int i = 0; i < word_count; i++)
            {
                string word = words[i];
                string tag = tags[i];
                Regex rgx = new Regex("<@[a-z0-9]+>");

                //continue if word pos is not in include list
                if (!include.Contains(tag) || word.Length < 3)
                    continue;

                //Remove user mention and smileys
                if (rgx.IsMatch(word) || smileyList.Contains(word))
                    continue;

                if (!wordDict.ContainsKey(word))
                {
                    CandidateWord candidate = new CandidateWord(word);
                    candidate.Index.Add(i);
                    wordDict[word] = candidate;
                }
                else
                {
                    CandidateWord candidate = wordDict[word];
                    candidate.Index.Add(i);
                }
            }

            return wordDict.Values.ToList();
        }

        /// <summary>
        /// Get the co-occurance graph. A candidate word can occur
        /// at multiple indices in the document.
        /// </summary>
        /// <param name="segmented_doc"></param>
        /// <param name="candidate_words"></param>
        /// <returns>A Graph with vertices as words in doc
        /// and edges between co-occuring words in distance of N </returns>
        private static List<Node> GetCooccurenceGraph(List<string> segmented_doc, List<CandidateWord> candidate_words)
        {
            int N = 5; //Distance parameter
            int n_candidate = candidate_words.Count;
            List<Node> graph = new List<Node>();

            try
            {
                //initialize graph with vertex and random score
                for (int i = 0; i < n_candidate; i++)
                {
                    Node node = new Node
                    {
                        Word = candidate_words[i],
                        Neighbours = new List<Node>(),
                    };

                    graph.Add(node);
                }

                //Build the graph, checking for co-occurence betweem every pair of words
                for (int i = 0; i < n_candidate; i++)
                {
                    for (int j = i + 1; j < n_candidate; j++)
                    {
                        CandidateWord word1 = candidate_words[i];
                        CandidateWord word2 = candidate_words[j];

                        List<int> occursAt1 = word1.Index;
                        List<int> occursAt2 = word2.Index;
                        int count1 = occursAt1.Count;
                        int count2 = occursAt2.Count;

                        //check for co-occurance of word1 & word2
                        for (int k = 0; k < count1; k++)
                        {
                            bool end = false;
                            for (int l = 0; l < count2; l++)
                            {
                                int index1 = occursAt1[k];
                                int index2 = occursAt2[l];

                                if (Math.Abs(index1 - index2) <= N)
                                {
                                    graph[i].Neighbours.Add(graph[j]);
                                    graph[j].Neighbours.Add(graph[i]);
                                    end = true;
                                    break;
                                }
                            }

                            if (end) break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return graph;
        }

        /// <summary>
        /// The main algorithmic step of Text Rank. Keeps on
        /// updating the vertex score until convergence is 
        /// reached. Convergence is considered if the differnce
        /// between of score of any vetex in consecutive iteration
        /// is less than a threshold value.
        /// </summary>
        /// <param name="segmentedDoc"></param>
        /// <param name="words"></param>
        /// <returns></returns>
        private static List<string> RankWords(List<string> segmentedDoc, List<CandidateWord> words)
        {
            List<Node> graph = GetCooccurenceGraph(segmentedDoc, words);
            bool _convergence_reached = false;
            double _convergence_threshold = 0.0001;
            double _dampning_factor = 0.85;
            int n_nodes = graph.Count;
            int n = n_nodes / 3;

            try
            {
                int iteration = 0;

                while (!_convergence_reached)
                {
                    iteration++;

                    for (int i = 0; i < n_nodes; i++)
                    {
                        Node node = graph[i];
                        List<Node> neighbours = node.Neighbours;
                        double initial_score = node.Word.Score;

                        node.Word.Score = 1 - _dampning_factor;

                        for (int j = 0; j < neighbours.Count; j++)
                        {
                            Node neighbour = neighbours[j];

                            node.Word.Score += _dampning_factor * neighbour.Word.Score / neighbour.Neighbours.Count;
                        }

                        if (!_convergence_reached && Math.Abs(node.Word.Score - initial_score) <= _convergence_threshold) { _convergence_reached = true; }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            // sort nodes in descending order by score
            graph.Sort((a, b) => -1 * a.Word.Score.CompareTo(b.Word.Score));

            //Select top n nodes and perform post processing
            var result = PostProcessing(graph.GetRange(0, Math.Min(n, n_nodes)).Select(node => node.Word).ToList());

            return result;
        }
        /// <summary>
        /// Performs post processing on top K candidate
        /// keywords. Adjacent keywords are collapsed into one.
        /// </summary>
        /// <param name="keywords"></param>
        /// <returns>List(string) of keywords</returns>
        private static List<string> PostProcessing (List<CandidateWord> keywords)
        {
            HashSet<string> finalKeywords = new HashSet<string>();
            List<Tuple<string, int>> words_with_index = new List<Tuple<string, int>>();
            int n_keywords = keywords.Count;

            foreach(CandidateWord key in keywords)
            {
                int occuranceCount = key.Index.Count;
                for (int i = 0; i < occuranceCount; i++)
                {
                    words_with_index.Add(Tuple.Create(key.Word, key.Index[i]));
                }
            }

            words_with_index.Sort((a, b) => a.Item2.CompareTo(b.Item2));

            string keyword = string.Empty;
            for(int i = 0; i < words_with_index.Count - 1; i++)
            {
                if (keyword == string.Empty)
                    keyword = words_with_index[i].Item1;

                Tuple<string, int> word1 = words_with_index[i];
                Tuple<string, int> word2 = words_with_index[i + 1];

                if (word2.Item2 == word1.Item2 + 1)
                {
                    keyword += " " + word2.Item1;
                }
                else
                {
                    finalKeywords.Add(keyword);
                    keyword = string.Empty;
                }
            }
            return finalKeywords.ToList();
        }
        #endregion

        #region Summarization

        public static string GetSummary(string doc)
        {
            string summary = "Not enough data for summarizing conversation";

            try
            {
                List<string> sentences = Helpers.SentenceSplitter(doc);
                sentences = FilterSentences(sentences);
                summary = SummarizeDoc(sentences);

                Logger.WriteToConsole("summary: \n" + summary);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return summary;
        }

        private static List<string> FilterSentences(List<string> sentences)
        {
            var result = new List<string>();
            int n_sentences = sentences.Count;
            List<int> toRemove = new List<int>();

            for(int i = 0; i < n_sentences; i++)
            {
                //Remove non user setences
                //Remove sentences with length <= 3 since it is unlikely they contain
                //any keyword or are part of summary.

                if (sentences[i].Contains("has joined the channel")) { continue; }
                if (sentences[i].Length <= 3) { continue; }

                result.Add(sentences[i]);
            }

            return result;
        }

        private static List<SentenceNode> GetSimilarityGraph(List<string> sentences)
        {
            int n_sentences = sentences.Count;
            List<SentenceNode> similarityGraph = new List<SentenceNode>();

            //Initialize vertices of the graph
            foreach (string sentence in sentences)
            {
                SentenceNode node = new SentenceNode
                {
                    Sentence = sentence,
                    Score = 1,
                    Neighbours = new List<Tuple<SentenceNode, double>>()
                };

                similarityGraph.Add(node);
            }

            //Build the graph and vertex neighbours
            for (int i = 0; i < n_sentences; i++)
            {
                for (int j = i + 1; j < n_sentences; j++)
                {
                    SentenceNode node1 = similarityGraph[i];
                    SentenceNode node2 = similarityGraph[j];

                    double similarity = GetSimilarityScore(node1.Sentence, node2.Sentence);

                    if (similarity != 0)
                    {
                        node1.Neighbours.Add(Tuple.Create(node2, similarity));
                        node2.Neighbours.Add(Tuple.Create(node1, similarity));
                    }
                }
            }

            return similarityGraph;
        }

        private static double GetSimilarityScore(string sentence1, string sentence2)
        {
            int n_common_words = 0;
            List<string> words1 = Helpers.SplitSentence(sentence1);
            List<string> words2 = Helpers.SplitSentence(sentence2);
            int wordCount1 = words1.Count;
            int wordCount2 = words2.Count;

            for (int i = 0; i < wordCount1; i++)
            {
                string word = words1[i];

                if (words2.Contains(word)) { n_common_words++; }
            }

            return n_common_words / (Math.Log(wordCount1) + Math.Log(wordCount2));
        }

        private static string SummarizeDoc(List<string> sentences)
        {
            List<SentenceNode> graph = GetSimilarityGraph(sentences);
            int sentence_count = graph.Count();

            double _dampning_factor = 0.85;
            double _convergence_threshold = 0.0001;
            bool _convergence_reached = false;
            int top_n = sentence_count / 3;

            Logger.WriteToConsole("Number of sentences: " + sentence_count);

            while (!_convergence_reached)
            {
                int iteration = 0;
                iteration++;

                for (int i = 0; i < sentence_count; i++)
                {
                    SentenceNode node = graph[i];
                    List<Tuple<SentenceNode, double>> neightbours = node.Neighbours;
                    int n_neighbour = neightbours.Count();
                    double initial_score = node.Score;
                    node.Score = 1 - _dampning_factor;

                    Logger.WriteToConsole("Iteration: " + iteration);
                    Logger.WriteToConsole("Update score for " + i + "th sentence");
                    for (int j = 0; j < n_neighbour; j++)
                    {
                        Tuple<SentenceNode, double> neighbour = neightbours[j];
                        SentenceNode n = neighbour.Item1;
                        double weight = neighbour.Item2;
                        double denominator = 0;
                        List<Tuple<SentenceNode, double>> n2 = n.Neighbours;
                        int n_count = n2.Count;
                        for (int k = 0; k < n_count; k++)
                        {
                            denominator += n2[k].Item2; 
                        }

                        node.Score += _dampning_factor * (weight / denominator) * n.Score;
                    }

                    if (!_convergence_reached && Math.Abs(node.Score - initial_score) <= _convergence_threshold) { _convergence_reached = true; }
                }
            }

            graph.Sort((a, b) => -1 * a.Score.CompareTo(b.Score));


            return string.Join("\n", graph.GetRange(0, top_n).Select(node => node.Sentence).ToList());
        }
        #endregion
    }

    #region internal class
    internal class CandidateWord
    {
        public string Word { get; set; }
        public double Score { get; set; }
        public List<int> Index { get; set; }

        public CandidateWord(string word, double score = 1.0)
        {
            Word = word;
            Index = new List<int>();
        }
    }

    internal class Node
    {
        public CandidateWord Word { get; set; }
        public List<Node> Neighbours { get; set; }
    }

    internal class SentenceNode
    {
        public string Sentence { get; set; }
        public double Score { get; set; }
        public List<Tuple<SentenceNode, double>> Neighbours;
    }
    #endregion
}
