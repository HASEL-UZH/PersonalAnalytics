// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-15
// 
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Windows;
using TaskDetectionTrackerTests;

namespace TaskDetectionTracker.Views.Tests
{
    [TestClass()]
    public class TaskDetectionPopupTests
    {
        [TestMethod()]
        public void TaskDetectionPopupTest()
        {
            var input = DataLoader.LoadTestData();
            input = DataMerger.MergeProcesses(input, TimeSpan.FromHours(1));
            var popup = (Window) new TaskDetectionPopup(input);
            popup.ShowDialog();
            
            Assert.Fail();
        }
        
    }
}