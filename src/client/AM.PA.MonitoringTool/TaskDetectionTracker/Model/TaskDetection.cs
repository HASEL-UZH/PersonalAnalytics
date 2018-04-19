// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-15
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TaskDetectionTracker.Model
{

    /// <summary>
    /// Model for the task detection. Returned by Katja's algorithm.
    /// </summary>
    public class TaskDetection : IComparable<TaskDetection>, INotifyPropertyChanged
    {
        private DateTime _start;
        private DateTime _end; //time of switch away
        private TaskType _taskTypePredicted; //set at the beginning and then not changed anymore
        private TaskType _taskTypeValidated; //empty at the beginning
        private bool _taskTypePredictedSet = false;
        private List<TaskDetectionInput> _timelineInfos;
        
        public DateTime Start { get { return _start; } set { _start = value; } }
        public DateTime End { get { return _end; } set { _end = value; } }
        public TaskType TaskTypePredicted { get { return _taskTypePredicted; } set { if (!_taskTypePredictedSet) { _taskTypePredicted = value; _taskTypePredictedSet = true; _taskTypeValidated = value; } } }
        public TaskType TaskTypeValidated { get { return _taskTypeValidated; } set { _taskTypeValidated = value; NotifyPropertyChanged("TaskTypeValidated"); } }
        public List<TaskDetectionInput> TimelineInfos { get { return _timelineInfos; } set { _timelineInfos = value; } }

        public TaskDetection()
        {
            // empty constructor
        }

        public TaskDetection(DateTime start, DateTime end, TaskType predicted, TaskType validated, List<TaskDetectionInput> infos)
        {
            Start = start;
            End = end;
            TaskTypePredicted = predicted;
            TaskTypeValidated = validated;
            TimelineInfos = infos;
        }

        public double Duration_InSeconds()
        {
            return Math.Abs((End - Start).TotalSeconds);
        }

        public int CompareTo(TaskDetection other)
        {
            return Start.CompareTo(other.Start); // TODO: add End as well in comparison?
        }

        public override string ToString()
        {
            return "predicted: " + TaskTypePredicted + ", validated: " + TaskTypeValidated + " [" + Start.ToShortTimeString() + " - " + End.ToShortTimeString() + "]"; // - (Main task: " + IsMainTask + ")";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}