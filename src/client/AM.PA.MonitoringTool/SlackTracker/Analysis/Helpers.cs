using System;
using System.Collections.Generic;
using SlackTracker.Data;
using SlackTracker.Data.SlackModel;
using OpenNLP.Tools.Tokenize;
using OpenNLP.Tools.SentenceDetect;
using OpenNLP.Tools.PosTagger;
using SlackTracker.Analysis;
using System.Linq;
using Shared;

namespace SlackTracker.Analysis
{
    internal class Helpers
    {
        #region Text Processing
        /// <summary>
        /// Gets Parts of Speech tags for words in document
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns>A list of POS tags</returns>
        public static List<string> GetPosTags(List<string> tokens)
        {
            List<string> tags = new List<string>();

            var _modelPath = AppDomain.CurrentDomain.BaseDirectory + "../../../SlackTracker/Analysis/resources/models/";

            try
            {
                var _posTagger = new EnglishMaximumEntropyPosTagger(_modelPath + "EnglishPOS.nbin", _modelPath + @"Parser\tagdict");

                tags.AddRange(_posTagger.Tag(tokens.ToArray()));
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return tags;
        }

        /// <summary>
        /// Tokenizes sentences into words. These words form
        /// the vertext of graph.
        /// </summary>
        /// <param name="sentences"></param>
        /// <returns>A list of words(string)</returns>
        public static List<string> Tokenize(List<string> sentences)
        {
            List<string> tokens = new List<string>();
            var _tokenizer = new EnglishRuleBasedTokenizer(false);

            foreach (string sentence in sentences)
            {
                tokens.AddRange(_tokenizer.Tokenize(sentence));
            }

            return tokens;
        }

        public static List<string> SplitSentence(string sentence)
        {
            var _tokenizer = new EnglishRuleBasedTokenizer(false);

            return _tokenizer.Tokenize(sentence).ToList();
        }
        /// <summary>
        /// Splits the document into sentences
        /// </summary>
        /// <param name="doc"></param>
        /// <returns>A List of sentences in doc</returns>
        public static List<string> SentenceSplitter(string doc)
        {
            var _modelPath = AppDomain.CurrentDomain.BaseDirectory + "../../../SlackTracker/Analysis/resources/models/";
            List<string> sentences = new List<string>();
            try
            {
                var _sentenceDetector = new EnglishMaximumEntropySentenceDetector(_modelPath + "EnglishSD.nbin");

                sentences.AddRange(_sentenceDetector.SentenceDetect(doc));
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return sentences;
        }
        #endregion

        #region Analysis helpers

        public static List<string> GetKeywordForDate (DateTime date, Channel channel)
        {
            List<string> keywords = null;
            try
            {
                List<LogData> log_for_date = DatabaseConnector.GetLog(date, channel.Id);
                string log = string.Join("\n", log_for_date.Select(p => p.Message));
                keywords = TextRank.GetKeywords(log);
            }
            catch(Exception e)
            {
                Logger.WriteToLogFile(e);
            }
            return keywords;
        }

        public static void GetKeywordsForWeek (DateTime date)
        {

        }

        public static string GetSummaryForDay (DateTime date, Channel channel)
        {
            string summary = "Not Enough Data";
            try
            {
                List<LogData> log_for_date = DatabaseConnector.GetLog(date, channel.Id);
                string log = string.Join("\n", log_for_date.Select(p => p.Message));
                summary = TextRank.GetSummary(log);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return summary;
        }

        public static void GetSummaryForWeek (DateTime date)
        {

        }
        #endregion
    }
}
