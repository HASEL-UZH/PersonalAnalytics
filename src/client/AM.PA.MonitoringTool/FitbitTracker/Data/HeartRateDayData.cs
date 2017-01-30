// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-25
// 
// Licensed under the MIT License.

using System;

namespace FitbitTracker.Data
{

    class HeartRateDayData
    {

        public DateTime Date { get; set; }

        public double RestingHeartrate { get; set; }

        public float CaloriesOut { get; set; }

        public int Max { get; set; }

        public int Min { get; set; }

        public int MinutesSpent { get; set; }

        public string Name { get; set; }

    }

}