﻿using System;
using System.Collections.Generic;
using SlackTracker.Data;
using SlackTracker.Data.SlackModel;
using SlackTracker.Analysis;
using System.Linq;
using Shared;

namespace SlackTracker.Analysis
{
    class Helpers
    {
        public static List<string> getKeywordForDate (DateTime date, Channel channel)
        {
            List<string> keywords = null;
            try
            {
                List<LogData> log_for_date = DatabaseConnector.GetLogForDate(date, channel);
                string log = string.Join("\n", log_for_date.Select(p => p.message));
                keywords = TopicSummarization.TextRank.getKeywords(log);
            }
            catch(Exception e)
            {
                Logger.WriteToLogFile(e);
            }
            return keywords;
        }

        public static void getKeywordsForWeek (DateTime date)
        {

        }

        public static string getSummaryForDay (DateTime date, Channel channel)
        {
            string summary = "Not Enough Data";
            try
            {
                List<LogData> log_for_date = DatabaseConnector.GetLogForDate(date, channel);
                string log = string.Join("\n", log_for_date.Select(p => p.message));
                summary = TopicSummarization.TextRank.getSummary(log);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return summary;
        }

        public static void getSummaryForWeek (DateTime date)
        {

        }
    }
}