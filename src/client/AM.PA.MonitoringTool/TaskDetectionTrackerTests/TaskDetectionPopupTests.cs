// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-15
// 
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Windows;
using TaskDetectionTrackerTests;
using TaskDetectionTracker.Algorithm;
using TaskDetectionTracker.Helpers;
using System.Threading;

namespace TaskDetectionTracker.Views.Tests
{
    [TestClass()]
    public class TaskDetectionPopupTests
    {

        private int _numberOfElementsPerTask = 3;
        private string[] tasks = { "research", "coding", "planning", "private", "meeting" };

        [TestMethod()]
        public void TaskDetectionPopupTest()
        {
            // fetch processes from demo data
            var processes = DataLoader.LoadTestData();
            processes = DataMerger.MergeProcesses(processes, TimeSpan.FromHours(1));
            int numberOfInput = processes.Count / _numberOfElementsPerTask + processes.Count % _numberOfElementsPerTask;

            // run task detection
            ITaskDetector td = new TaskDetectorImpl();
            var input = td.FindTasks(processes);

            // run popup
            var popup = (Window)new TaskDetectionPopup(input);
            popup.ShowDialog();
        }
        
    }
}