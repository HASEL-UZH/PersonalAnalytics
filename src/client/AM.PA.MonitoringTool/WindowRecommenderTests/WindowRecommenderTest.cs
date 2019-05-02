using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.QualityTools.Testing.Fakes;
using WindowRecommender;
using WindowRecommender.Graphics;
using WindowRecommender.Util.Fakes;

namespace WindowRecommenderTests
{
    [TestClass]
    public class WindowRecommenderTest
    {
        [TestMethod]
        public void TestGetScoredWindows_BothEmpty()
        {
            var scores = new Dictionary<IntPtr, double>();
            var windowStack = new List<WindowRecord>();
            var expectedScoredWindows = new List<(WindowRecord windowRecord, bool show)>();
            var actualScoredWindows = WindowRecommender.WindowRecommender.GetScoredWindows(scores, windowStack).ToList();
            CollectionAssert.AreEqual(expectedScoredWindows, actualScoredWindows);
        }

        [TestMethod]
        public void TestGetScoredWindows_ScoresEmpty()
        {
            var scores = new Dictionary<IntPtr, double>();
            var windowStack = new List<WindowRecord>
            {
                new WindowRecord(new IntPtr(1)),
                new WindowRecord(new IntPtr(2)),
            };
            var expectedScoredWindows = new List<(WindowRecord windowRecord, bool show)>
            {
                (windowRecord: new WindowRecord(new IntPtr(1)), show: true),
            };
            var actualScoredWindows = WindowRecommender.WindowRecommender.GetScoredWindows(scores, windowStack).ToList();
            CollectionAssert.AreEqual(expectedScoredWindows, actualScoredWindows);
        }

        [TestMethod]
        public void TestGetScoredWindows_StackEmpty()
        {
            var scores = new Dictionary<IntPtr, double>
            {
                { new IntPtr(1), 0.6},
                { new IntPtr(2), 0.4},
            };
            var windowStack = new List<WindowRecord>();
            var expectedScoredWindows = new List<(WindowRecord windowRecord, bool show)>();
            var actualScoredWindows = WindowRecommender.WindowRecommender.GetScoredWindows(scores, windowStack).ToList();
            CollectionAssert.AreEqual(expectedScoredWindows, actualScoredWindows);
        }

        [TestMethod]
        public void TestGetScoredWindows()
        {
            var scores = new Dictionary<IntPtr, double>
            {
                { new IntPtr(1), 0.6},
                { new IntPtr(2), 0.4},
            };
            var windowStack = new List<WindowRecord>
            {
                new WindowRecord(new IntPtr(1)),
                new WindowRecord(new IntPtr(2)),
            };
            var expectedScoredWindows = new List<(WindowRecord windowRecord, bool show)>
            {
                (windowRecord: new WindowRecord(new IntPtr(1)), show: true),
                (windowRecord: new WindowRecord(new IntPtr(2)), show: true),
            };
            var actualScoredWindows = WindowRecommender.WindowRecommender.GetScoredWindows(scores, windowStack).ToList();
            CollectionAssert.AreEqual(expectedScoredWindows, actualScoredWindows);
        }

        [TestMethod]
        public void TestGetScoredWindows_Hidden()
        {
            var scores = new Dictionary<IntPtr, double>
            {
                { new IntPtr(3), 1},
            };
            var windowStack = new List<WindowRecord>
            {
                new WindowRecord(new IntPtr(1)),
                new WindowRecord(new IntPtr(2)),
                new WindowRecord(new IntPtr(3)),
            };
            var expectedScoredWindows = new List<(WindowRecord windowRecord, bool show)>
            {
                (windowRecord: new WindowRecord(new IntPtr(1)), show: true),
                (windowRecord: new WindowRecord(new IntPtr(2)), show: false),
                (windowRecord: new WindowRecord(new IntPtr(3)), show: true),
            };
            var actualScoredWindows = WindowRecommender.WindowRecommender.GetScoredWindows(scores, windowStack).ToList();
            CollectionAssert.AreEqual(expectedScoredWindows, actualScoredWindows);
        }

        [TestMethod]
        public void TestGetScoredWindows_NumberOfWindows()
        {
            // Check Settings as test depends on value of 3
            Assert.AreEqual(3, Settings.NumberOfWindows);

            var scores = new Dictionary<IntPtr, double>
            {
                { new IntPtr(1), 0.8},
                { new IntPtr(2), 0.6},
                { new IntPtr(3), 0.4},
                { new IntPtr(4), 0.2},
            };
            var windowStack = new List<WindowRecord>
            {
                new WindowRecord(new IntPtr(1)),
                new WindowRecord(new IntPtr(2)),
                new WindowRecord(new IntPtr(3)),
                new WindowRecord(new IntPtr(4)),
                new WindowRecord(new IntPtr(5)),
            };
            var expectedScoredWindows = new List<(WindowRecord windowRecord, bool show)>
            {
                (windowRecord: new WindowRecord(new IntPtr(1)), show: true),
                (windowRecord: new WindowRecord(new IntPtr(2)), show: true),
                (windowRecord: new WindowRecord(new IntPtr(3)), show: true),
            };
            var actualScoredWindows = WindowRecommender.WindowRecommender.GetScoredWindows(scores, windowStack).ToList();
            CollectionAssert.AreEqual(expectedScoredWindows, actualScoredWindows);
        }

        [TestMethod]
        public void TestGetScoredWindows_NumberOfWindows_WithoutForeground()
        {
            // Check Settings as test depends on value of 3
            Assert.AreEqual(3, Settings.NumberOfWindows);

            var scores = new Dictionary<IntPtr, double>
            {
                { new IntPtr(4), 0.8},
                { new IntPtr(3), 0.6},
                { new IntPtr(2), 0.4},
            };
            var windowStack = new List<WindowRecord>
            {
                new WindowRecord(new IntPtr(1)),
                new WindowRecord(new IntPtr(2)),
                new WindowRecord(new IntPtr(3)),
                new WindowRecord(new IntPtr(4)),
                new WindowRecord(new IntPtr(5)),
            };
            var expectedScoredWindows = new List<(WindowRecord windowRecord, bool show)>
            {
                (windowRecord: new WindowRecord(new IntPtr(1)), show: true),
                (windowRecord: new WindowRecord(new IntPtr(2)), show: false),
                (windowRecord: new WindowRecord(new IntPtr(3)), show: true),
                (windowRecord: new WindowRecord(new IntPtr(4)), show: true),
            };
            var actualScoredWindows = WindowRecommender.WindowRecommender.GetScoredWindows(scores, windowStack).ToList();
            CollectionAssert.AreEqual(expectedScoredWindows, actualScoredWindows);
        }

        [TestMethod]
        public void TestGetDrawList()
        {
            using (ShimsContext.Create())
            {
                ShimWindowUtils.GetCorrectedWindowRectangleWindowRecord = record => new Rectangle(1, 1, 1, 1);
                var expectedDrawList = new List<(Rectangle rectangle, bool show)>
                {
                    (rectangle: new Rectangle(1, 1, 1, 1), show: true)
                };
                var actualDrawList = WindowRecommender.WindowRecommender.GetDrawList(new List<(WindowRecord windowRecord, bool show)>
                {
                    (windowRecord: new WindowRecord(new IntPtr(1)), show: true),
                }).ToList();
                CollectionAssert.AreEqual(expectedDrawList, actualDrawList);
            }
        }
    }
}
