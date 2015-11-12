// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.Collections.Generic;

namespace UserEfficiencyTracker.Models
{
    /// <summary>
    /// Class to transfer the needed information from the background thread
    /// to the survey window.
    /// </summary>
    public class NeededSurveyWindowData
    {
        public DateTime PreviousSurveyTimestamp { get; set; }

        public int PreviousSurveyEntryProductivity { get; set; }
        public int PreviousSurveyEntrySatisfaction { get; set; }
        public int PreviousSurveyEntryEmotions { get; set; }
        public int PreviousSurveyEntryTaskDifficulty { get; set; }
        public int PreviousSurveyEntrySlowNetwork { get; set; }

        public int PreviousSurveyEntryInterruptibility { get; set; }
        public List<string> PreviousSurveyEntryTasksWorkedOn { get; set; }
        public DateTime CurrentSurveyEntryNotificationTimeStamp { get; set; }

        public NeededSurveyWindowData()
        {
            PreviousSurveyEntryTasksWorkedOn = new List<string>();
        }
    }
}
