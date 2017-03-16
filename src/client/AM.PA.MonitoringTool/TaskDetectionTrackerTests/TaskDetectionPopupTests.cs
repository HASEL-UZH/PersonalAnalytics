// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-15
// 
// Licensed under the MIT License.

using Microsoft.VisualBasic.FileIO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using TaskDetectionTracker.Model;
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
            var popup = (Window) new TaskDetectionPopup(new ObservableCollection<TaskDetectionInput>(input));
            popup.ShowDialog();
            
            Assert.Fail();
        }
        
    }
}