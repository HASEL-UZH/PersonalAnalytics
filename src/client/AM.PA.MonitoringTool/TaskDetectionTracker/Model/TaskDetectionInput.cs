// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-15
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace TaskDetectionTracker.Model
{
    public class TaskDetectionInput : IComparable<TaskDetectionInput>
    {
        private DateTime _start;
        private DateTime _end;
        private string _processName;
        private List<string> _windowTitles = new List<string>();
        private int _numberOfKeystrokes;
        private int _numberOfMouseClicks;

        public DateTime Start { get { return _start; } set { _start = value; } }
        public DateTime End { get { return _end; } set { _end = value; } }
        public string ProcessName { get { return _processName; } set { _processName = value; } }
        public List<string> WindowTitles { get { return _windowTitles; } set { _windowTitles = value; } }
        public int NumberOfKeystrokes { get { return _numberOfKeystrokes; } set { _numberOfKeystrokes = value; } }
        public int NumberOfMouseClicks { get { return _numberOfMouseClicks; } set { _numberOfMouseClicks = value; } }

        public override string ToString()
        {
            return ProcessName + ": " + Start.ToLongTimeString() + " - " + End.ToLongTimeString();
        }
        
        public int CompareTo(TaskDetectionInput other)
        {
            return Start.CompareTo(other.Start);
        }
    }
}