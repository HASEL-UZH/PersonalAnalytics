using System;
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
            try
            {
                List<Log> log_for_date = DatabaseConnector.GetLogForDate(date, channel);
                string log = string.Join("\n", log_for_date.Select(p => p.message));
                List<string> keywords = TopicSummarization.TextRank.getKeywords(log);

                Logger.WriteToConsole(log);
            }
            catch(Exception e)
            {
                Logger.WriteToLogFile(e);
            }
            return new List<string>();
        }

        public static void getKeywordsForWeek (DateTime date)
        {

        }

        public static void getSummaryForDay (DateTime date)
        {

        }

        public static void getSummaryForWeek (DateTime date)
        {

        }
    }
}
