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
        public void TestGetWindowInfo_Empty()
        {
            var scores = new Dictionary<IntPtr, double>();
            var windowStack = new IntPtr[0];
            var windowInfo = new List<(Rectangle rect, bool show)>();
            CollectionAssert.AreEqual(windowInfo, WindowRecommender.WindowRecommender.GetWindowInfo(scores, windowStack).ToList());
        }

        [TestMethod]
        public void TestGetWindowInfo()
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
                    (new Rectangle(3, 3, 3, 3), true),
                };
                var actualWindowInfo = WindowRecommender.WindowRecommender.GetWindowInfo(scores, windowStack).ToList();
                CollectionAssert.AreEqual(expectedWindowInfo, actualWindowInfo);
            }
        }
    }
}
