// Created by Rohit Kaushik (f20150115@goa.bits-pilani.ac.in) at the University of Zurich
// Created: 2018-07-13
// 
// Licensed under the MIT License.

using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenNLP;
using OpenNLP.Tools.Tokenize;
using OpenNLP.Tools.SentenceDetect;
using OpenNLP.Tools.PosTagger;

namespace SlackTracker.Analysis.TopicSummarization
{
    class TextRank
    {

        //perform Text Rank algo on doc and retrieves keywords
        public static List<string> getKeywords (string doc, int n = 10)
        {
            List<string> keywords = null;

            try
            {
                List<string> words = tokenize(sentenceSplitter(doc));
                List<string> tags = getPosTags(words);

                List<candidateWord> candidateWords = filterWords(words, tags);
                keywords = rankWords(words, candidateWords);

                foreach(string word in keywords)
                {
                    Logger.WriteToConsole(word + "\n");
                }
            }
            catch(Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return keywords;
        }

        private static List<string> getPosTags (List<string> tokens)
        {
            List<string> tags = new List<string>();

            var _modelPath = AppDomain.CurrentDomain.BaseDirectory + "../../../SlackTracker/Analysis/resources/models/";

            try
            {
                var _posTagger = new EnglishMaximumEntropyPosTagger(_modelPath + "EnglishPOS.nbin", _modelPath + @"Parser\tagdict");

                tags.AddRange(_posTagger.Tag(tokens.ToArray()));
            }
            catch(Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return tags;
        }

        private static List<string> tokenize (List<string> sentences)
        {
            List<string> tokens = new List<string>();
            var _tokenizer = new EnglishRuleBasedTokenizer(false);

            foreach (string sentence in sentences)
            {
                tokens.AddRange(_tokenizer.Tokenize(sentence));
            }

            return tokens;
        }

        private static List<string> sentenceSplitter(string doc)
        {
            var _modelPath = AppDomain.CurrentDomain.BaseDirectory + "../../../SlackTracker/Analysis/resources/models/";
            List<string> sentences = new List<string>();
            try
            {
                var _sentenceDetector = new EnglishMaximumEntropySentenceDetector(_modelPath + "EnglishSD.nbin");

                sentences.AddRange(_sentenceDetector.SentenceDetect(doc));
            }
            catch(Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return sentences;
        }

        /// <summary>
        /// </summary>
        /// <param name="taggeddoc"></param>
        /// <param name="noun"></param>
        /// <param name="verbs"></param>
        /// <param name="adjectives"></param>
        /// <returns></returns>
        private static List<candidateWord> filterWords (List<string> words, List<string> tags)
        {
            //List of POS tags to be included in candidate keywords
            //List<string> include = new List<string>() { "JJ", "NN", "NNP", "NNS", "VB", "VBN", "VBG"};
            List<string> include = new List<string>() { "JJ", "NN", "NNP", "NNS"};
            List<candidateWord> candidate_words = new List<candidateWord>();
            int word_count = words.Count;
            int tag_count = tags.Count;
            int i = 0;

            if (word_count != tag_count)
            {
                throw new Exception("words and tags list do not match");
            }
                
            for (i = 0; i < word_count; i++)
            {
                string word = words[i];
                string tag = tags[i];

                if (include.Contains(tag))
                {
                    candidateWord candidate = new candidateWord { word = word, index = i };
                    candidate_words.Add(candidate);
                }
            }

            return candidate_words;
        }

        private static List<Node> getCooccurenceGraph(List<string> segmented_doc, List<candidateWord> candidate_words)
        {
            int N = 5; //Distance parameter
            int n_candidate = candidate_words.Count;
            List<Node> graph = new List<Node>();
            Random random_score = new Random();

            try
            {
                //initialize graph with vertex and random score
                for (int i = 0; i < n_candidate; i++)
                {
                    Node node = new Node
                    {
                        word = candidate_words[i],
                        score = 1,
                        neighbours = new List<Node>(),
                    };

                    graph.Add(node);
                }

                for (int i = 0; i < n_candidate; i++)
                {
                    for (int j = i + 1; j < n_candidate; j++)
                    {
                        candidateWord word1 = candidate_words[i];
                        candidateWord word2 = candidate_words[j];

                        int index1 = word1.index;
                        int index2 = word2.index;

                        //check if they co-occur in distance of N in the document
                        if (Math.Abs(index1 - index2) <= N)
                        {
                            graph[i].neighbours.Add(graph[j]);
                            graph[j].neighbours.Add(graph[i]);
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

        private static List<string> rankWords(List<string> segmentedDoc, List<candidateWord> words)
        {
            List<Node> graph = getCooccurenceGraph(segmentedDoc, words);
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

                    Logger.WriteToConsole("Iteration: " + iteration);

                    Logger.WriteToConsole("Number of candidate: " + n_nodes);

                    for (int i = 0; i < n_nodes; i++)
                    {
                        Node node = graph[i];
                        List<Node> neighbours = node.neighbours;
                        double initial_score = node.score;

                        node.score = 1 - _dampning_factor;

                        Logger.WriteToConsole("Update score for: " + node.word.word);

                        Logger.WriteToConsole("Initial Score for candidate" + i + " is: " + initial_score);
                        Logger.WriteToConsole("Number of neighbours for candidate: " + node.neighbours.Count);

                        for (int j = 0; j < neighbours.Count; j++)
                        {
                            Node neighbour = neighbours[j];

                            node.score += _dampning_factor * neighbour.score / neighbour.neighbours.Count;
                        }

                        Logger.WriteToConsole(node.word.word + " has score " + node.score);
                        if (!_convergence_reached && Math.Abs(node.score - initial_score) <= _convergence_threshold) { _convergence_reached = true; }
                    }

                    Logger.WriteToConsole(_convergence_reached ? "convergance reached" : "still iterating");
                }
            }
            catch(Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            // sort nodes in descending order by score
            graph.Sort((a, b) => -1 * a.score.CompareTo(b.score));

            return postProcessing(graph.GetRange(0, Math.Min (n, n_nodes)).Select(node => node.word).ToList());
        }

        private static List<string> postProcessing (List<candidateWord> keywords)
        {
            List<string> finalKeywords = new List<string>();
            int n_keywords = keywords.Count;

            //sort by index
            keywords.Sort((a, b) => a.index.CompareTo(b.index));

            string keyword = string.Empty;

            //Collapse adjacent keywords
            for(int i = 0; i < n_keywords - 1; i++)
            {
                if (keyword == string.Empty) { keyword = keywords[i].word; }

                if (keywords[i+1].index - keywords[i].index == 1) { keyword = keyword + " " + keywords[i + 1].word;}
                else { finalKeywords.Add(keyword); keyword = string.Empty; }
            }

            return finalKeywords;
        }
    }

    internal class candidateWord
    {
        public string word { get; set; }
        public int index { get; set; }
    }

    internal class Node
    {
        public candidateWord word { get; set; }
        public double score { get; set; }
        public List<Node> neighbours { get; set; }
    }
}
