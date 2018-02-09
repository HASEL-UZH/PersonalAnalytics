// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-15
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Shared.Helpers;

namespace TaskDetectionTracker.Model
{

    /// <summary>
    /// Input model for Katja's algorithm
    /// </summary>
    public class TaskDetectionInput : IComparable<TaskDetectionInput>
    {
        private DateTime _start;
        private DateTime _end;
        private string _processName;
        private string _processNameFormatted;
        private List<string> _windowTitles = new List<string>();
        private int _numberOfKeystrokes;
        private int _numberOfMouseClicks;

        public DateTime Start { get { return _start; } set { _start = value; } }
        public DateTime End { get { return _end; } set { _end = value; } }
        public string ProcessName { get { return _processName; } set { _processName = value; _processNameFormatted = ProcessNameHelper.GetFileDescription(_processName); } }
        public string ProcessNameFormatted { get { return _processNameFormatted; } set { _processNameFormatted = value; } }
        public List<string> WindowTitles { get { return _windowTitles; } set { _windowTitles = value; } }
        public int NumberOfKeystrokes { get { return _numberOfKeystrokes; } set { _numberOfKeystrokes = value; } }
        public int NumberOfMouseClicks { get { return _numberOfMouseClicks; } set { _numberOfMouseClicks = value; } }


        public TaskDetectionInput()
        {
            // empty constructor
        }

        public TaskDetectionInput(DateTime start, DateTime end, string processName, List<string> windowTitles, int keystrokes, int clicks)
        {
            Start = start;
            End = end;
            ProcessName = processName;
            ProcessNameFormatted = ProcessNameHelper.GetFileDescription(ProcessName);
            WindowTitles = windowTitles;
            NumberOfKeystrokes = keystrokes;
            NumberOfMouseClicks = clicks;
        }

        public double Duration_InSeconds()
        {
            return Math.Abs((End - Start).TotalSeconds);
        }

        public override string ToString()
        {
            return string.Format("Process: {0} ({1} to {2}, dur: {3}) keys: {4} clicks: {5}",
                ProcessName,
                Start.ToLongTimeString(),
                End.ToLongTimeString(),
                End.Subtract(Start).ToString(),
                NumberOfKeystrokes,
                NumberOfMouseClicks);
        }
        
        public int CompareTo(TaskDetectionInput other)
        {
            return Start.CompareTo(other.Start);
        }
    }
}