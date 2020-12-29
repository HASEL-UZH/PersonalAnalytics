// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-23
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace FitbitTracker.Model
{
    public class SleepLog
    {
        private double minutesToFallAsleep = double.NaN;

        public int AwakeCount { get; set; }
        public int AwakeDuration { get; set; }
        public int AwekeningsCount { get; set; }
        public DateTime DateOfSleep { get; set; }
        public long Duration { get; set; }
        public int Efficiency { get; set; }
        public bool IsMainSleep { get; set; }
        public string LogID { get; set; }
        public int MinutesAfterWakeup { get; set; }
        public int MinutesAsleep { get; set; }
        public int MinutesAwake { get; set; }
        public double MinutesToFallAsleep { get { return minutesToFallAsleep; } set { minutesToFallAsleep = value; } }
        public int RestlessCount { get; set; }
        public int RestlessDuration { get; set; }
        public DateTime StartTime { get; set; }
        public int TimeInBed { get; set; }
        public List<SleepIntraData> MinuteData { get; set; }
    }
}