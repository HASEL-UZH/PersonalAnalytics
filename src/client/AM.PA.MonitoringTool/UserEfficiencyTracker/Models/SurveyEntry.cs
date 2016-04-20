// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;

namespace UserEfficiencyTracker.Models
{
    public class SurveyEntry
    {
        public DateTime PreviousWorkDay { get; set; }
        public DateTime TimeStampNotification { get; set; }
        public DateTime TimeStampStarted { get; set; }
        public DateTime TimeStampFinished { get; set; }

        public int Productivity { get; set; } // 1-7

        public SurveyEntry()
        {
            TimeStampStarted = DateTime.Now;
        }
    }
}
