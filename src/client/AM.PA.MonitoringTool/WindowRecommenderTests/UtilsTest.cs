using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using WindowRecommender;

namespace WindowRecommenderTests
{
    [TestClass]
    public class UtilsTest
    {
        [TestMethod]
        public void TestPairwise()
        {
            var source = new[] { "a", "b", "c", "d" };
            var result = source.Pairwise((one, two) => one + two);
            CollectionAssert.AreEqual(new[] { "ab", "bc", "cd" }, result.ToArray());
        }

        [TestMethod]
        public void TestPairwise_Empty()
        {
            var source = new string[0];
            var result = source.Pairwise((one, two) => one + two);
            CollectionAssert.AreEqual(new string[0], result.ToArray());
        }

        [TestMethod]
        public void TestPairwise_Single()
        {
            var source = new[] { "a" };
            var result = source.Pairwise((one, two) => one + two);
            CollectionAssert.AreEqual(new string[0], result.ToArray());
        }

        [TestMethod]
        public void TestGetTopEntries_Empty()
        {
            var scores = new Dictionary<IntPtr, double>();
            var topWindows = Utils.GetTopEntries(scores, 3);
            Assert.IsTrue(Enumerable.SequenceEqual(Enumerable.Empty<IntPtr>(), topWindows));
        }

        [TestMethod]
        public void TestGetTopEntries_Count()
        {
            var count = 3;
            var scores = new Dictionary<IntPtr, double>();
            for (var i = 1; i < count + 2; i++)
            {
                scores[new IntPtr(i)] = 1;
            }
            var topWindows = Utils.GetTopEntries(scores, count);
            Assert.AreEqual(Settings.NumberOfWindows, topWindows.Count());
        }
    }
}
