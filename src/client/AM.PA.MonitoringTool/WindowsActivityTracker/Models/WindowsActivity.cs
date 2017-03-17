// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-04
// 
// Licensed under the MIT License.

using System;

namespace WindowsActivityTracker.Models
{
    public class WindowsActivity
    {
        public int Id { get; set; }
        public string Participant { get; set; }
        public int EventNumber { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationInSeconds { get; set; }
        public string WindowTitle { get; set; }
        public string ProcessName { get; set; }
        public ActivityCategory ActivityCategory { get; set; }
    }
}
