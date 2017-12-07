// Created by André Meyer at MSR
// Created: 2015-12-16
// 
// Licensed under the MIT License.

using Shared.Data.Extractors;
using System;
using System.Linq;
using System.Collections.Generic;
using Shared.Data;
using TimeSpentVisualizer.Data;
using TimeSpentVisualizer.Models;

namespace TimeSpentVisualizer.Helpers
{
    public static class CollectData
    {
        private const int MinDurationInMins = 1;

        /// <summary>
        /// Fetches the visited websites, cleanes it for the MinDuration. 
        /// Hint: no need to sort, as they already comed sorted by duration.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal static List<TimeSpentItem> GetCleanedWebsites(DateTimeOffset date)
        {
            var maxWebsites = 25; 
            var websites = WindowTitleWebsitesExtractor.GetWebsitesVisited(date);
            var websitesFiltered = websites.Where(w => w.DurationInMins > MinDurationInMins).Take(maxWebsites).ToList();

            var list = new List<TimeSpentItem>();
            foreach (var w in websitesFiltered)
            {
                var item = new TimeSpentItem(TimeSpentType.Website, w.ItemName, w.DurationInMins);
                list.Add(item);
            }

            return list;
        }

        /// <summary>
        /// Fetches files, cleanes them for the MinDuration.
        /// Hint: no need to sort, as they already comed sorted by duration.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal static List<TimeSpentItem> GetCleanedFilesWorkedOn(DateTimeOffset date)
        {
            var maxFiles = 20;
            var files = WindowTitleArtifactExtractor.GetFilesWorkedOn(date);
            var filesFiltered = files.Where(w => w.DurationInMins > MinDurationInMins).Take(maxFiles).ToList();

            var list = new List<TimeSpentItem>();
            foreach (var w in filesFiltered)
            {
                var title = w.FileName; // + w.FilePath;
                var item = new TimeSpentItem(TimeSpentType.File, title, w.DurationInMins);
                list.Add(item);
            }

            return list;

            //WindowTitleArtifactExtractor.GetFileExtensionFromFileName(file.FileName),
            //        WindowTitleArtifactExtractor.GetFileNameWithoutExtension(file.FileName), 
            //        Math.Round(file.DurationInMins, 1));
        }

        /// <summary>
        /// Fetches VS projects, cleanes them for the MinDuration.
        /// Hint: no need to sort, as they already comed sorted by duration.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal static List<TimeSpentItem> GetCleanedVisualStudioProjects(DateTimeOffset date)
        {
            var maxVsProjects = 10;
            var vsProjects = WindowTitleCodeExtractor.GetVisualStudioProjects(date);

            var list = new List<TimeSpentItem>();
            foreach (var p in vsProjects)
            {
                var item = new TimeSpentItem(TimeSpentType.VsProject, p.Key, p.Value);
                list.Add(item);
            }

            return list.Where(w => w.DurationInMins > MinDurationInMins).Take(maxVsProjects).ToList();
        }

        /// <summary>
        /// Fetches code reviews, cleanes them for the MinDuration.
        /// Hint: no need to sort, as they already comed sorted by duration.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal static List<TimeSpentItem> GetCleanedCodeReviewsDone(DateTimeOffset date)
        {
            var maxCodeReviews = 15;
            var reviews = WindowTitleCodeExtractor.GetCodeReviewsDone(date);

            var list = new List<TimeSpentItem>();
            foreach (var p in reviews)
            {
                var item = new TimeSpentItem(TimeSpentType.CodeReview, p.Key, p.Value);
                list.Add(item);
            }

            return list.Where(w => w.DurationInMins > MinDurationInMins).Take(maxCodeReviews).ToList();
        }

        /// <summary>
        /// Fetches programs used for the date
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal static List<TimeSpentItem> GetCleanedPrograms(DateTimeOffset date)
        {
            var programs = Queries.GetActivityPieChartData(date);

            var list = new List<TimeSpentItem>();
            foreach (var p in programs)
            {
                var item = new TimeSpentItem(TimeSpentType.Programs, p.Key, p.Value);
                list.Add(item);
            }

            var sortedList = list.OrderByDescending(i => i.DurationInMins).ToList();

            return sortedList.Where(w => w.DurationInMins > MinDurationInMins).ToList();
        }

        /// <summary>
        /// Fetches information about the time spent in Outlook
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal static List<TimeSpentItem> GetCleanedOutlookInfo(DateTimeOffset date)
        {
            var emailDetails = WindowTitleEmailExtractor.GetTimeSpentInOutlook(date);

            var list = new List<TimeSpentItem>();
            foreach (var p in emailDetails)
            {
                var item = new TimeSpentItem(TimeSpentType.Outlook, p.ItemName, p.DurationInMins);
                list.Add(item);
            }

            return list.Where(w => w.DurationInMins > MinDurationInMins).ToList();
        }

        /// <summary>
        /// Fetches meetings from the DB 
        /// (hint: no API calls are made, meetings are only fetched if they are already stored)
        /// </summary>
        /// <param name="date"></param>
        /// <param name="hideMeetingsWithoutAttendees"></param>
        /// <returns></returns>
        internal static List<TimeSpentItem> GetCleanedMeetings(DateTimeOffset date, bool hideMeetingsWithoutAttendees)
        {
            var meetingsFromDb = Queries.GetMeetingsFromDatabase(date);

            // if already saved, get it from the database (if not today)
            var list = new List<TimeSpentItem>();
            if (meetingsFromDb == null || meetingsFromDb.Count <= 0) return list;

            foreach (var w in meetingsFromDb)
            {
                // hide day or longer meetings
                if (w.Item3 >= 24 * 60) continue;

                // hide meetings which not yet occurred
                if (w.Item2 > DateTime.Now) continue;

                // (optionally) hide meetings where no attendees
                if (hideMeetingsWithoutAttendees && w.Item4 == 0) continue;

                var item = new TimeSpentItem(TimeSpentType.Meeting, w.Item1, w.Item3);
                list.Add(item);
            }

            return list;
        }
    }
}
