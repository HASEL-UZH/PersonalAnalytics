// Created by Katja Kevic (kevic@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-21
// 
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskDetectionTracker.Model;

namespace TaskDetectionTracker.Algorithm
{
    public interface ITaskDetector
    {
        List<TaskDetection> FindTasks(List<TaskDetectionInput> processes);
    }
}
