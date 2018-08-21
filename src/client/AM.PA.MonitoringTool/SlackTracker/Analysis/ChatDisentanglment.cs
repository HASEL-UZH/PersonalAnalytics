using SlackTracker.Data.SlackModel;
using System;
using System.Collections.Generic;
using Accord.IO;
using Accord.Statistics.Models.Regression;
using System.Linq;
using SlackTracker.Analysis.TopicSummarization;
using Shared;

namespace SlackTracker.Analysis
{
    class ChatDisentanglment
    {
        public static List<Cluster> getThreads(List<LogData> messages)
        {
            List<Cluster> threads = new List<Cluster>();

            try
            {
                LogisticRegression classifier = Serializer.Load<LogisticRegression>(AppDomain.CurrentDomain.BaseDirectory + "../../../SlackTracker/Analysis/resources/models/threadClassifier");
                List<string> keywords = TextRank.getKeywords(string.Join(" ", messages.Select(m => m.message)));
                int k = 0;

                foreach (LogData message in messages)
                {
                    double optimal_vote = 0;
                    int optimal_cluster_index = 0;

                    for (int c = 1; c < k; c++)
                    {
                        Cluster thread = threads[c - 1];
                        List<LogData> m = thread.messages;

                        double quality = 0;

                        foreach (LogData l in m)
                        {
                            quality += classifier.Probabilities(feature_vector(message, l, keywords))[1];
                        }

                        if (quality > optimal_vote)
                        {
                            optimal_cluster_index = c;
                            optimal_vote = quality;
                        }
                    }

                    if (optimal_vote > 0.5)
                    {
                        threads[optimal_cluster_index].messages.Add(message);
                    }
                    else
                    {
                        Cluster thread = new Cluster();
                        thread.messages.Add(message);
                        threads.Add(thread);
                        k++; //increase count of number of clusters
                    }
                }
            }
            catch(Exception e)
            {
                Logger.WriteToLogFile(e);
            }


            return threads;
        }

        private static double[] feature_vector(LogData message1, LogData message2, List<string> keywords_for_doc)
        {
            double[] feature = new double[16];
            List<string> greetings = new List<string>() { "Hi", "Hello", "Hey"};
            List<string> thanks = new List<string>() { "Thanks", "Thankyou", "Thanks", "Thnx" };
            List<string> answer = new List<string>() { "yes", "no", "nopes", "yeah" };

            //swap message1 with message2 if message1 is sent after message2
            if (DateTime.Compare(message1.timestamp, message2.timestamp) > 0)
            {
                LogData temp = message1;
                message1 = message2;
                message2 = temp;
            }

            feature[0] = (message2.timestamp - message1.timestamp).TotalSeconds; //time difference between messages
            feature[1] = message1.author == message2.author ? 1.0 : 0.0; //same author
            feature[2] = message1.mentions.Contains(message2.author) ? 1.0 : 0.0;//author of message1 mentions author of message2
            feature[3] = message2.mentions.Contains(message1.author) ? 1.0 : 0.0; //author of message2 mentions author of message1
            feature[4] = message1.mentions.Intersect(message2.mentions).Any() ? 1.0 : 0.0; //mentions same users
            feature[5] = message1.message.Split().Intersect(greetings).Any() ? 1.0 : 0.0;//message1 uses greetings
            feature[6] = message2.message.Split().Intersect(greetings).Any() ? 1.0 : 0.0; //message2 uses greetings
            feature[7] = message1.message.Split().Count() > 10 ? 1.0 : 0.0; //message1 is long > 10 words
            feature[8] = message2.message.Split().Count() > 10 ? 1.0 : 0.0; //message2 is long > 10 words
            feature[9] = message2.message.Split().Intersect(thanks).Any() ? 1.0 : 0.0; //message2 uses greetings
            feature[10] = message2.message.Split().Intersect(thanks).Any() ? 1.0 : 0.0; //message2 uses greetings
            feature[11] = message1.message.Contains("?") ? 1.0 : 0.0; //message1 contains question
            feature[12] = message2.message.Contains("?") ? 1.0 : 0.0; //message2 contains question
            feature[13] = message2.message.Split().Intersect(answer).Any() ? 1.0 : 0.0; //message1 is an answer to a previous message
            feature[14] = message2.message.Split().Intersect(answer).Any() ? 1.0 : 0.0; //message2 is an answer to a previous message
            feature[15] = keyword_similarity(message1.message, message2.message, keywords_for_doc); //keyword similarity
            Logger.WriteToConsole(feature[0] + " : " + feature[15]);
            return feature;
        }

        #region Helpers
        private static double keyword_similarity(string m1, string m2, List<string> keywords_for_doc)
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

    internal class Cluster
    {
        public List<LogData> messages { get; set; }

        public Cluster()
        {
            messages = new List<LogData>();
        }
    }
}
