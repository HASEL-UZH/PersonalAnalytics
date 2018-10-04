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
    class Activity
    {
        public static List<UserInteraction> GetUserInteractions(List<LogData> logs)
        {
            List<UserInteraction> activities = new List<UserInteraction>();
            HashSet<string> users = new HashSet<string>();

            int n_users = 0;

            //get all the users that participated in the chat
            foreach (LogData log in logs)
            {
                users.Add(log.author);
                foreach (string mention in log.mentions)
                {
                    users.Add(mention);
                }
            }

            List<string> user_list = users.ToList();
            n_users = users.Count;
            //now for each user

            foreach (LogData log in logs)
            {
                if (log.author == "") continue;
                string channel_name = DatabaseConnector.GetChannelNameFromId(log.channel_id);
                string author_name = DatabaseConnector.GetUserNameFromId(log.author);
                foreach (string mention in log.mentions)
                {
                    if (mention == "") continue;
                    string mention_name = DatabaseConnector.GetUserNameFromId(mention);

                    UserInteraction activity = activities.FirstOrDefault(p =>
                        p.from == author_name && p.to == mention_name && p.channel_name == channel_name && p.date.ToString(Settings.FORMAT_DAY) == log.timestamp.ToString(Settings.FORMAT_DAY));

                    if (activity == null)
                    {
                        activity = new UserInteraction();
                        activity.channel_name = channel_name;
                        activity.from = author_name;
                        activity.to = mention_name;
                        activity.duration = log.message.Length * 5;
                        activity.topics = new HashSet<string>(TextRank.getKeywords(log.message.ToLower()));
                        activity.date = log.timestamp;
                        activities.Add(activity);
                    }
                    else
                    {
                        activity.duration += log.message.Length * 5;
                        List<string> keys = TextRank.getKeywords(log.message.ToLower());
                        foreach (string key in keys)
                        {
                            activity.topics.Add(key);
                        }
                    }
                }
            }

            foreach(UserInteraction a in activities)
            {
                Logger.WriteToConsole(a.from + " to " + a.to + " duration " + a.duration + " topics " + string.Join(", ", a.topics));
            }
            return activities;
        }

        public static List<UserActivity> GetUserActivity(List<LogData> data)
        {
            List<UserActivity> result = new List<UserActivity>();

            foreach (LogData log in data)
            {
                if (log.mentions == null || log.mentions.Count == 0)
                {
                    continue;
                }

                foreach (string mention in log.mentions)
                {
                    UserActivity activity = new UserActivity();
                    activity.time = log.timestamp;
                    activity.from = log.author;
                    activity.to = mention;
                    activity.intensity = 3;
                    result.Add(activity);
                }
            }
            return result;
        }

        public static List<string> getTopicsFromMessage(string log, List<string> keywords)
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
