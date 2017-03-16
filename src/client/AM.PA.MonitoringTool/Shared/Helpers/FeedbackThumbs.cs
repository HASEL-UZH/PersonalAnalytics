// Created by André Meyer at MSR
// Created: 2016-01-14
// 
// Licensed under the MIT License.

using Shared.Data;
using System;
using System.Globalization;

namespace Shared.Helpers
{
    public sealed class FeedbackThumbs
    {
        private static FeedbackThumbs _feedback;

        private FeedbackThumbs()
        {
            // also call if not enabled (to be sure to have it)
            CreateFeedbackTable();
        }

        /// <summary>
        /// Singleton
        /// </summary>
        /// <returns></returns>
        public static FeedbackThumbs GetInstance()
        {
            return _feedback ?? (_feedback = new FeedbackThumbs());
        }

        private static void CreateFeedbackTable()
        {
            try
            {
                Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.FeedbackDbTable + " (id INTEGER PRIMARY KEY, time TEXT, visName TEXT, visType TEXT, visDate TEXT, likes INTEGER, dislikes INTEGER)");
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Save thumbs up or down for selected visualization (type & date)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="date"></param>
        /// <param name="visName"></param>
        /// <param name="up"></param>
        /// <param name="down"></param>
        public void SetFeedback(VisType type, DateTimeOffset date, string visName, int up, int down)
        {
            try
            {
                    var query = "INSERT INTO " + Settings.FeedbackDbTable + " (time, visName, visType, visDate, likes, dislikes) VALUES (strftime('%Y-%m-%d %H:%M:%f', 'now', 'localtime'), " +
                    Database.GetInstance().Q(visName) + ", " + Database.GetInstance().Q(type.ToString()) + ", " + Database.GetInstance().QTime(date.Date) + ", " + 
                    Database.GetInstance().Q(up)  + ", " + Database.GetInstance().Q(down) + ");";
                Database.GetInstance().ExecuteDefaultQuery(query);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Get html to add on each visualization dashboard item to show
        /// thumbs up/down for feedback
        /// </summary>
        /// <param name="vis"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public string GetFeedbackThumbsUpDown(IVisualization vis, DateTimeOffset date)
        {
            if (!Settings.IsFeedbackEnabled) return string.Empty;

            var html = "<div class='thumbsOverlay'>"
                    + "<input class=\"thumb\" type=\"image\"  src=\"thumbUpGray.png\" onmouseover=\"this.src='thumbUpBlue.png'\" onmouseout=\"this.src='thumbUpGray.png'\" onclick =\"window.external.JS_ThumbsVote('up', '{0}', '{1}', '{2}')\" /> "
                    + "<input class=\"thumb\" type=\"image\"  src=\"thumbDownGray.png\" onmouseover=\"this.src='thumbDownBlue.png'\" onmouseout=\"this.src='thumbDownGray.png'\" onclick =\"window.external.JS_ThumbsVote('down', '{0}', '{1}', '{2}')\" />"
                    + "</div>";

            return string.Format(CultureInfo.InvariantCulture, html, VisHelper.CreateChartHtmlTitle(vis.Title), vis.Type, date);
        }
    }
}
