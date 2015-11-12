// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using System;
using Shared.Data;

namespace UserEfficiencyTracker
{
    public static class Settings
    {
        public static bool IsEnabled = Database.GetInstanceSettings().MiniSurveysEnabled;
        public const string DbTable = "user_efficiency_survey";

        //private const double DefaultMinutes = 60.0; //every 2h
        private const double PostponeShortInMinutes = 5.0; // every 5mins
        private const double SurveyCheckerMinutes = 1.0; // every minute

        //public static TimeSpan DefaultInterval = GetDefaultInterval(); 
        public static TimeSpan PostponeShortInterval = TimeSpan.FromMinutes(PostponeShortInMinutes);
        public static TimeSpan SurveyCheckerInterval = TimeSpan.FromMinutes(SurveyCheckerMinutes);

        /// <summary>
        /// the number of previously inserted tasks shown to the user in the autocompletion box
        /// </summary>
        public const int NumberOfPreviousTasksShown = 50;

        /// <summary>
        /// get the default interval from the settings
        /// (not a property to udpate it every time it's needed; TODO: not so efficient!)
        /// </summary>
        /// <returns></returns>
        public static TimeSpan GetDefaultInterval()
        {
            return TimeSpan.FromMinutes(Database.GetInstanceSettings().MiniSurveyInterval);
        }
    }
}
