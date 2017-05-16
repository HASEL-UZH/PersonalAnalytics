// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-16
// 
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TaskDetectionTracker.Helpers;
using TaskDetectionTrackerTests;

namespace TaskDetectionTracker.Tests
{
    [TestClass()]
    public class DataMergerTests
    {
        [TestMethod()]
        public void MergeProcessesTest()
        {
            var input = DataLoader.LoadTestData();
            var mergedProcesses = DataMerger.MergeProcesses(input, TimeSpan.FromHours(1));
            
            //TODO: Write actual tests

            Assert.Fail();
        }
    }
}