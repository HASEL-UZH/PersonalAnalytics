// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;

namespace UserEfficiencyTracker.Models
{
    public class SurveyEntry
    {
        public DateTime TimeStampNotification { get; set; }
        public DateTime TimeStampStarted { get; private set; }
        public DateTime TimeStampFinished { get; set; }
        public int Productivity { get; set; }

        public int Satisfaction { get; set; }

        public int Emotions { get; set; }

        public int TaskDifficulty { get; set; }

        public string TasksWorkedOn { get; set; }

        public int Interruptibility { get; set; }

        public int SlowNetwork { get; set; }

        public string Comments { get; set; }

        public SurveyEntry()
        {
            TimeStampStarted = DateTime.Now;
        }
    }
}
