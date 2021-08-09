using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Math;
using Microsoft.ML;
using NHunspell;
using System.IO;
using System.Text.RegularExpressions;

namespace WindowsActivityTracker.TaskDetection
{
    class Task
    {

        private Dictionary<string, int> bag;
        private string taskRepresentation = "";
        private int taskNum;
        double[] aveVector;
        private int startTime;
        private int endTime;
        private double topPercentile;
        private string path = @"C:\Users\pcgou\OneDrive\Documents\UBCResearch\GoogleNews-vectors-negative300-SLIM.bin\GoogleNews-vectors-negative300-SLIM.bin";
        // private string path = @"C:\Users\pcgou\OneDrive\Documents\UBCResearch\SO_vectors_200.bin";

        public Task(List<string> windowTitles, double topPercentile)
        {
            this.topPercentile = topPercentile;
            createBagOfWords(windowTitles);
            createAverageVector();
        }

        /// <summary>
        /// Creates and initalizes average word2vec vector for task
        /// </summary>
        /// <param name="newTask"></param>
        /// <returns></returns>
        private void createAverageVector()
        {
            var vocabulary = new Word2vec.Tools.Word2VecBinaryReader().Read(path);
            int vecSize = vocabulary.VectorDimensionsCount;
            double[] vector = new double[vecSize];
            int numTokens = 0;

            foreach (KeyValuePair<string, int> token in bag)
            {
                try
                {
                    double[] tokenVector = vocabulary.GetRepresentationFor(token.Key).NumericVector.ToDouble();
                    numTokens++;
                    for (int i = 0; i < vecSize; i++)
                    {
                        vector[i] += tokenVector[i];
                    }
                }
                catch
                {
                    // Do nothing for unkown words                   
                }
            }

            // Divide each entry in vector by the number of tokens to get average
            for (int i = 0; i < vecSize; i++)
            {
                vector[i] /= numTokens;
            }
            this.aveVector = vector;
        }

        /// <summary>
        /// Addes a bag of words to this
        /// </summary>
        /// <param name="newBag"></param>
        /// <returns></returns>
        public void collapseTasks(Task newBag, int secondsToAdd)
        {
            Dictionary<string, int> toAdd = newBag.getBag();
            foreach (KeyValuePair<string, int> token in toAdd)
            {
                if (bag.ContainsKey(token.Key))
                {
                    bag[token.Key] = bag[token.Key] + toAdd[token.Key];
                }
                else
                {
                    bag.Add(token.Key, token.Value);
                }
            }
            endTime += secondsToAdd;
            removeInfrequentWords();
            createAverageVector();
        }

        /// <summary>
        /// Creates bag of words
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public void createBagOfWords(List<string> data)
        {
            List<List<string>> cleanedData = clean(data);
            this.bag = new Dictionary<string, int>();

            foreach (List<string> windowTitle in cleanedData)
            {
                foreach (string token in windowTitle)
                {

                    if (bag.ContainsKey(token))
                    {
                        bag[token] = bag[token] + 1;
                    }
                    else
                    {
                        bag.Add(token, 1);
                    }
                }
            }
            removeInfrequentWords();
        }

        #region data cleaning

        /// <summary>
        /// Cleans data set 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public List<List<string>> clean(List<string> data)
        {
            List<List<string>> cleanedText = new List<List<string>>(); // List of lists of tokens

            foreach (string windowTitle in data)
            {
                // Tokenize, remove stop words and remove non english words
                string[] temp = tokenize(windowTitle);
                if (temp != null)
                {
                    List<string> cleanedWindowTitle = removeNonEnglishWords(tokenize(windowTitle)); // List of tokens based off the window title
                    cleanedText.Add(cleanedWindowTitle);
                }
            }
            return cleanedText;
        }

        /// <summary>
        /// Removes non English words and splits camel case words
        /// </summary>
        /// <param name="words"></param>
        /// <returns></returns>
        private List<string> removeNonEnglishWords(string[] words)
        {

           // EnglishStemmer stemmer = new EnglishStemmer();
            Hunspell hunspell = new Hunspell(@"C:\Users\pcgou\source\repos\TaskSummarization\TaskSummarization\bin\x64\Debug\en_us.aff", @"C:\Users\pcgou\source\repos\TaskSummarization\TaskSummarization\bin\x64\Debug\en_us.dic");
            List<string> englishWords = new List<string>();
            if (words.Length > 0)
            {
                foreach (string word in words)
                {
                    string camelCase = Regex.Replace(Regex.Replace(word, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2"); // Splits camel case token into seperate words
                    string[] tokens = camelCase.Split(' ');
                    foreach (string token in tokens)
                    {
                        if (!Regex.IsMatch(token.ToLower(), "google") && !Regex.IsMatch(token.ToLower(), "search") && !Regex.IsMatch(token.ToLower(), "com") && !Regex.IsMatch(token.ToLower(), "ca")) // remove google search *temporary*
                        {
                            if (hunspell.Spell(token) && !Regex.IsMatch(token, @"^\d+$")) // If word is a correct English word
                            {
                                englishWords.Add(token.ToLower());
                            }
                        }
                    }
                }
            }
            return englishWords;
        }

        /// <summary>
        /// Tokenizes and removes all stop words
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private string[] tokenize(string text)
        {

            MLContext context = new MLContext();
            var emptyData = new List<TextData>();
            var data = context.Data.LoadFromEnumerable(emptyData);
            var tokenization = context.Transforms.Text.TokenizeIntoWords("Tokens", "Text", separators: new[] { ' ', ',', '-', '_', '.', ':' })
                .Append(context.Transforms.Text.RemoveDefaultStopWords("Tokens", "Tokens",
                    Microsoft.ML.Transforms.Text.StopWordsRemovingEstimator.Language.English));

            var stopWordsModel = tokenization.Fit(data);
            var engine = context.Model.CreatePredictionEngine<TextData, TextTokens>(stopWordsModel);
            var newText = engine.Predict(new TextData { Text = text });

            return newText.Tokens;

        }

        /// <summary>
        /// Deletes the least occuring words based on topPercentile
        /// </summary>
        /// <param name="bagOfWords"></param>>
        /// <returns></returns>
        public void removeInfrequentWords()
        {
            var importantWords = bag.ToList();
            importantWords.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
            int upperBound = (int)(importantWords.Count * (1 - topPercentile));
            importantWords.RemoveRange(0, upperBound);
            this.bag = importantWords.ToDictionary(x => x.Key, x => x.Value);
            try
            {
                for (int i = 0; i < 4; i++)
                {
                    taskRepresentation += importantWords[importantWords.Count - 1 - i].Key + " | ";
                }
                taskRepresentation += importantWords[importantWords.Count - 1 - 5].Key;
            } catch
            {

            }
        }



        #endregion


        #region Getters and Setters

        public Dictionary<string, int> getBag()
        {
            return this.bag;
        }

        public void setTaskNum(int number)
        {
            this.taskNum = number;
        }

        public string getRepresentation()
        {
            return this.taskRepresentation;
        }

        public int getTaskNum()
        {
            return this.taskNum;
        }

        public void setTimes(int startTime, int endTime)
        {
            this.startTime = startTime;
            this.endTime = endTime;
        }

        public double[] getVector()
        {
            return this.aveVector;
        }

        public int getStartTime()
        {
            return this.startTime;
        }

        public int getEndTime()
        {
            return this.endTime;
        }
        #endregion

        private class TextData
        {
            public string Text { get; set; }
        }

        private class TextTokens : TextData
        {
            public string[] Tokens { get; set; }
        }
    }
}
