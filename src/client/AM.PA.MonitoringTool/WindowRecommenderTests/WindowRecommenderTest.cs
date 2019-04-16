using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using WindowRecommender;
using WindowRecommender.Native;
using WindowRecommender.Native.Fakes;

namespace WindowRecommenderTests
{
    [TestClass]
    public class WindowRecommenderTest
    {

        [TestMethod]
        public void TestGetDrawList_Empty()
        {
            var scores = new Dictionary<IntPtr, double>();
            var windowStack = new IntPtr[0];
            var windowInfo = new List<(Rectangle rect, bool show)>();
            CollectionAssert.AreEqual(windowInfo, WindowRecommender.WindowRecommender.GetDrawList(scores, windowStack).ToList());
        }

        [TestMethod]
        public void TestGetDrawList()
        {
            using (ShimsContext.Create())
            {
                ShimNativeMethods.GetWindowRectangleIntPtr = windowHandle =>
                {
                    var i = (int)windowHandle;
                    return new RECT(i, i, i, i);
                };

                var scores = new Dictionary<IntPtr, double>();
                var windowStack = new List<IntPtr>();
                for (var i = 1; i < Settings.NumberOfWindows + 2; i++)
                {
                    var windowHandle = new IntPtr(i);
                    windowStack.Add(windowHandle);
                    scores[windowHandle] = 1;
                }

                var expectedWindowInfo = new List<(Rectangle rect, bool show)>
                {
                    (new Rectangle(1, 1, 1, 1), true),
                    (new Rectangle(2, 2, 2, 2), true),
                    (new Rectangle(3, 3, 3, 3), true)
                };
                var actualWindowInfo = WindowRecommender.WindowRecommender.GetDrawList(scores, windowStack).ToList();
                CollectionAssert.AreEqual(expectedWindowInfo, actualWindowInfo);
            }
        }

        [TestMethod]
        public void TestGetDrawList_LimitedScores()
        {
            using (ShimsContext.Create())
            {
                ShimNativeMethods.GetWindowRectangleIntPtr = windowHandle =>
                {
                    var i = (int)windowHandle;
                    return new RECT(i, i, i, i);
                };
                var scores = new Dictionary<IntPtr, double>
                {
                    {new IntPtr(3), 1},
                    {new IntPtr(1), 1}
                };
                var windowStack = new[]
                {
                    new IntPtr(1),
                    new IntPtr(2),
                    new IntPtr(3),
                    new IntPtr(4)
                };
                var expectedWindowInfo = new List<(Rectangle rect, bool show)>
                {
                    (new Rectangle(1, 1, 1, 1), true),
                    (new Rectangle(2, 2, 2, 2), false),
                    (new Rectangle(3, 3, 3, 3), true)
                };
                var actualWindowInfo = WindowRecommender.WindowRecommender.GetDrawList(scores, windowStack).ToList();
                CollectionAssert.AreEqual(expectedWindowInfo, actualWindowInfo);
            }
        }

        [TestMethod]
        public void TestGetDrawList_ForegroundMissing()
        {
            using (ShimsContext.Create())
            {
                ShimNativeMethods.GetWindowRectangleIntPtr = windowHandle =>
                {
                    var i = (int)windowHandle;
                    return new RECT(i, i, i, i);
                };

                var scores = new Dictionary<IntPtr, double>();
                var windowStack = new List<IntPtr>();
                for (var i = 1; i < Settings.NumberOfWindows + 4; i++)
                {
                    var windowHandle = new IntPtr(i);
                    windowStack.Add(windowHandle);
                    if (i > 2)
                    {
                        scores[windowHandle] = 1;
                    }
                }

                var expectedWindowInfo = new List<(Rectangle rect, bool show)>
                {
                    (new Rectangle(1, 1, 1, 1), true),
                    (new Rectangle(2, 2, 2, 2), false),
                    (new Rectangle(3, 3, 3, 3), true),
                    (new Rectangle(4, 4, 4, 4), true)
                };
                var actualWindowInfo = WindowRecommender.WindowRecommender.GetDrawList(scores, windowStack).ToList();
                CollectionAssert.AreEqual(expectedWindowInfo, actualWindowInfo);
            }
        }
    }
}
