// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-26
// 
// Licensed under the MIT License.

namespace FitbitTracker.Data.FitbitModel
{
    public class ActivitySummary
    {

        public int ActiveScore { get; set; }

        public float Elevation { get; set; }

        public int FairlyActiveMinutes { get; set; }

        public int Floors { get; set; }

        public int LightlyActiveMinutes { get; set; }

        public int SedentaryMinutes { get; set; }

        public int Steps { get; set; }

        public int VeryActiveMinutes { get; set; }

    }
}