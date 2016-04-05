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
        public static bool DefaultPopUpIsEnabled = true;
        public const int DefaultPopUpInterval = 60; // in minutes

        //public static bool IsEnabled = MiniSurveysEnabled;
        public const string DbTable = "user_efficiency_survey";

        //private const double DefaultMinutes = 60.0; //every 2h
        private const double PostponeShortInMinutes = 5.0; // every 5mins
        private const double SurveyCheckerMinutes = 1.0; // every minute

        //public static TimeSpan DefaultInterval = GetDefaultInterval(); 
        public static TimeSpan PostponeShortInterval = TimeSpan.FromMinutes(PostponeShortInMinutes);
        public static TimeSpan SurveyCheckerInterval = TimeSpan.FromMinutes(SurveyCheckerMinutes);
    }
}
