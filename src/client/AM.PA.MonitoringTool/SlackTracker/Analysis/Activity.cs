using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;
using SlackTracker.Data;
using SlackTracker.Data.SlackModel;

namespace SlackTracker.Analysis
{
    internal class Activity
    {
        public static List<UserInteraction> GetUserInteractions(List<LogData> logs)
        {
            List<UserInteraction> activities = new List<UserInteraction>();
            HashSet<string> users = new HashSet<string>();

            int n_users = 0;

            //get all the users that participated in the chat
            foreach (LogData log in logs)
            {
                users.Add(log.Author);
                foreach (string mention in log.Mentions)
                {
                    users.Add(mention);
                }
            }

            List<string> user_list = users.ToList();
            n_users = users.Count;
            //now for each user

            foreach (LogData log in logs)
            {
                if (log.Author == "") continue;
                string channel_name = DatabaseConnector.GetChannelNameFromId(log.ChannelId);
                string author_name = DatabaseConnector.GetUserNameFromId(log.Author);
                foreach (string mention in log.Mentions)
                {
                    if (mention == "") continue;
                    string mention_name = DatabaseConnector.GetUserNameFromId(mention);

                    UserInteraction activity = activities.FirstOrDefault(p =>
                        p.From == author_name && p.To == mention_name && p.ChannelName == channel_name && p.Date.ToString(Settings.FORMAT_DAY) == log.Timestamp.ToString(Settings.FORMAT_DAY));

                    if (activity == null)
                    {
                        activity = new UserInteraction();
                        activity.ChannelName = channel_name;
                        activity.From = author_name;
                        activity.To = mention_name;
                        activity.Duration = log.Message.Length * 5;
                        activity.Topics = new HashSet<string>(TextRank.GetKeywords(log.Message.ToLower()));
                        activity.Date = log.Timestamp;
                        activities.Add(activity);
                    }
                    else
                    {
                        activity.Duration += log.Message.Length * 5;
                        List<string> keys = TextRank.GetKeywords(log.Message.ToLower());
                        foreach (string key in keys)
                        {
                            activity.Topics.Add(key);
                        }
                    }
                }
            }

            foreach(UserInteraction a in activities)
            {
                Logger.WriteToConsole(a.From + " to " + a.To + " duration " + a.Duration + " topics " + string.Join(", ", a.Topics));
            }
            return activities;
        }

        public static List<UserActivity> GetUserActivity(List<LogData> data)
        {
            List<UserActivity> result = new List<UserActivity>();

            foreach (LogData log in data)
            {
                if (log.Mentions == null || log.Mentions.Count == 0)
                {
                    continue;
                }

                foreach (string mention in log.Mentions)
                {
                    UserActivity activity = new UserActivity();
                    activity.Time = log.Timestamp;
                    activity.From = log.Author;
                    activity.To = mention;
                    activity.Intensity = 3;
                    result.Add(activity);
                }
            }
            return result;
        }

        public static List<string> GetTopicsFromMessage(string log, List<string> keywords)
        {
            List<string> topics = new List<string>();
            foreach (string key in keywords)
            {
                if(log.Contains(key)) { topics.Add(key);}
            }
            return topics;
        }
    }
}
