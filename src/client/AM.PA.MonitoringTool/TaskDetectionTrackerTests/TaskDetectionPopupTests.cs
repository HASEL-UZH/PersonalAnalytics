// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-15
// 
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Windows;
using TaskDetectionTracker.Model;
using TaskDetectionTrackerTests;
using System.Linq;

namespace TaskDetectionTracker.Views.Tests
{
    [TestClass()]
    public class TaskDetectionPopupTests
    {

        private int _numberOfElementsPerTask = 3;
        private string[] tasks = { "research", "coding", "planning", "private"};

        [TestMethod()]
        public void TaskDetectionPopupTest()
        {
            var processes = DataLoader.LoadTestData();
            processes = DataMerger.MergeProcesses(processes, TimeSpan.FromHours(1));
            
            int numberOfInput = processes.Count / _numberOfElementsPerTask + processes.Count % _numberOfElementsPerTask;

            var input = new List<TaskDetection>();
            for (int i = 0; i < numberOfInput; i++)
            {
                TaskDetection tdInput = new TaskDetection();
                var prc = processes.GetRange(i * _numberOfElementsPerTask, _numberOfElementsPerTask);
                tdInput.Start = prc.First().Start;
                tdInput.End = prc.Last().End;
                tdInput.TaskTypeProposed = tasks[i % tasks.Length];
                tdInput.TimelineInfos = prc;
                input.Add(tdInput);
            }
           
            var popup = (Window) new TaskDetectionPopup(input);
            popup.ShowDialog();
            
            Assert.Fail();
        }
        
    }
}