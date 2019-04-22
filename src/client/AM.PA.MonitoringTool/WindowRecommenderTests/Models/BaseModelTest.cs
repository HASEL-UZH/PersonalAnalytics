using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowRecommender;
using WindowRecommender.Models;

namespace WindowRecommenderTests
{
    [TestClass]
    public class BaseModelTest
    {
        [TestMethod]
        public void TestGetTopWindows_Empty()
        {
            var scores = new Dictionary<IntPtr, double>();
            var topWindows = BaseModel.GetTopWindows(scores);
            Assert.IsTrue(Enumerable.Empty<IntPtr>().SequenceEqual(topWindows));
        }

        [TestMethod]
        public void TestGetTopWindows_Count()
        {
            var scores = new Dictionary<IntPtr, double>();
            for (var i = 1; i < Settings.NumberOfWindows + 2; i++)
            {
                scores[new IntPtr(i)] = 1;
            }
            var topWindows = BaseModel.GetTopWindows(scores);
            Assert.AreEqual(Settings.NumberOfWindows, topWindows.Count());
        }
    }
}
