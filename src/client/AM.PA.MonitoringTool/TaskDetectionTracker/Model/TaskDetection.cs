// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-15
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace TaskDetectionTracker.Model
{
    public class TaskDetection
    {
        private DateTime _start;
        private DateTime _end; //time of switch away
        private string _taskTypeProposed;
        private string _taskTypeValidated; //empty at the beginning
        private TaskDetectionCase _taskDetectionCase;
        private List<TaskDetectionInput> _timelineInfos;
        
        public DateTime Start { get { return _start; } set { _start = value; } }
        public DateTime End { get { return _end; } set { _end = value; } }
        public string TaskTypeProposed { get { return _taskTypeProposed; } set { _taskTypeProposed = value; } }
        public string TaskTypeValidated { get { return _taskTypeValidated; } set { _taskTypeValidated = value; } }
        public TaskDetectionCase TaskDetectionCase { get { return _taskDetectionCase; } set { _taskDetectionCase = value; } }
        public List<TaskDetectionInput> TimelineInfos { get { return _timelineInfos; } set { _timelineInfos = value; } }
    }
}