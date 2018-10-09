using SlackTracker.Data.SlackModel;
using System;
using System.Collections.Generic;
using Accord.IO;
using Accord.Statistics.Models.Regression;
using System.Linq;
using SlackTracker.Analysis;
using Shared;

namespace SlackTracker.Analysis
{
    internal class ChatDisentanglment
    {
        public static List<Thread> GetThreads(List<LogData> messages, bool detailed = false)
        {
            List<Thread> threads = new List<Thread>();

            try
            {
                LogisticRegression classifier = Serializer.Load<LogisticRegression>(AppDomain.CurrentDomain.BaseDirectory + "../../../SlackTracker/Analysis/resources/models/threadClassifier");
                List<string> keywords = TextRank.GetKeywords(string.Join(" ", messages.Select(m => m.Message)));
                int k = 0;

                foreach (LogData message in messages)
                {
                    double optimal_vote = 0;
                    int optimal_cluster_index = 0;

                    for (int c = 1; c < k; c++)
                    {
                        Thread thread = threads[c - 1];
                        List<LogData> m = thread.Messages;

                        double quality = 0;

                        foreach (LogData l in m)
                        {
                            quality += classifier.Probabilities(FeatureVector(message, l, keywords))[1];
                        }

                        if (quality > optimal_vote)
                        {
                            optimal_cluster_index = c;
                            optimal_vote = quality;
                        }
                    }

                    if (optimal_vote > 0.5)
                    {
                        threads[optimal_cluster_index].Messages.Add(message);
                    }
                    else
                    {
                        Thread thread = new Thread();
                        thread.Messages.Add(message);
                        threads.Add(thread);
                        k++; //increase count of number of clusters
                    }
                }
                if (detailed) { PostAnalysis(threads); }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return threads;
        }

        private static double[] FeatureVector(LogData message1, LogData message2, List<string> keywords_for_doc)
        {
            double[] feature = new double[16];
            List<string> greetings = new List<string>() { "Hi", "Hello", "Hey" };
            List<string> thanks = new List<string>() { "Thanks", "Thankyou", "Thanks", "Thnx" };
            List<string> answer = new List<string>() { "yes", "no", "nopes", "yeah" };

            //swap message1 with message2 if message1 is sent after message2
            if (DateTime.Compare(message1.Timestamp, message2.Timestamp) > 0)
            {
                LogData temp = message1;
                message1 = message2;
                message2 = temp;
            }

            feature[0] = (message2.Timestamp - message1.Timestamp).TotalSeconds; //time difference between messages
            feature[1] = message1.Author == message2.Author ? 1.0 : 0.0; //same author
            feature[2] = message1.Mentions.Contains(message2.Author) ? 1.0 : 0.0;//author of message1 mentions author of message2
            feature[3] = message2.Mentions.Contains(message1.Author) ? 1.0 : 0.0; //author of message2 mentions author of message1
            feature[4] = message1.Mentions.Intersect(message2.Mentions).Any() ? 1.0 : 0.0; //mentions same users
            feature[5] = message1.Message.Split().Intersect(greetings).Any() ? 1.0 : 0.0;//message1 uses greetings
            feature[6] = message2.Message.Split().Intersect(greetings).Any() ? 1.0 : 0.0; //message2 uses greetings
            feature[7] = message1.Message.Split().Count() > 10 ? 1.0 : 0.0; //message1 is long > 10 words
            feature[8] = message2.Message.Split().Count() > 10 ? 1.0 : 0.0; //message2 is long > 10 words
            feature[9] = message2.Message.Split().Intersect(thanks).Any() ? 1.0 : 0.0; //message2 uses greetings
            feature[10] = message2.Message.Split().Intersect(thanks).Any() ? 1.0 : 0.0; //message2 uses greetings
            feature[11] = message1.Message.Contains("?") ? 1.0 : 0.0; //message1 contains question
            feature[12] = message2.Message.Contains("?") ? 1.0 : 0.0; //message2 contains question
            feature[13] = message2.Message.Split().Intersect(answer).Any() ? 1.0 : 0.0; //message1 is an answer to a previous message
            feature[14] = message2.Message.Split().Intersect(answer).Any() ? 1.0 : 0.0; //message2 is an answer to a previous message
            feature[15] = KeywordSimilarity(message1.Message, message2.Message, keywords_for_doc); //keyword similarity
            return feature;
        }

        public static void PostAnalysis(List<Thread> threads)
        {
            foreach(Thread thread in threads)
            {
                List<LogData> SortedList = thread.Messages.OrderBy(o => o.Timestamp).ToList();
                thread.StartTime = SortedList.First().Timestamp;
                thread.EndTime = SortedList.Last().Timestamp;

                List<string> author = SortedList.Select(m => m.Author).ToList();
                List<List<string>> mentions = SortedList.Select(m => m.Mentions).ToList();
                HashSet<string> user_participated = new HashSet<string>();
                foreach (string user in author)
                {
                    user_participated.Add(user);
                }

                foreach (List<string> mention in mentions)
                {
                    foreach (string user in mention)
                    {
                        user_participated.Add(user);
                    }
                }

                thread.UserParticipated = user_participated.ToList();
                thread.Keywords = TextRank.GetKeywords(string.Join(" ", SortedList.Select(l => l.Message).ToList()));
            }
        }

        #region Helpers
        private static double KeywordSimilarity(string m1, string m2, List<string> keywords_for_doc)
        {
            List<string> words1 = m1.Split(' ').ToList();
            List<string> words2 = m2.Split(' ').ToList();
            List<string> keys1 = words1.Intersect(keywords_for_doc).ToList();
            List<string> keys2 = words2.Intersect(keywords_for_doc).ToList();

            if (keys1.Count == 0 || keys2.Count == 0) { return 0.0; }

            return (keys1.Intersect(keys2)).ToList().Count / Math.Max(keys1.Count, keys2.Count);
        }
        #endregion
    }
}
