using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlackTracker.Data.SlackModel;

namespace SlackTracker.Analysis
{
    class Activity
    {
        public static List<UserActivity> GetUserActivities(List<Thread> threads)
        {
            List<UserActivity> activities = new List<UserActivity>();
            foreach (Thread thread in threads)
            {
                foreach (LogData log in thread.messages)
                {
                    foreach (string mention in log.mentions)
                    {
                        UserActivity activity = new UserActivity();
                        activity.channel_id = log.channel_id;
                        activity.from = log.author;
                        activity.to = mention;
                        activity.words = log.message.Split(' ').ToList();
                        activity.start_time = thread.start_time;
                        activity.end_time = thread.end_time;

                        activities.Add(activity);
                    }
                }
            }
            return activities;
        }
    }
}
