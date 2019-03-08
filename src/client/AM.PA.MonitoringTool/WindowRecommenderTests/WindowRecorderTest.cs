using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using WindowRecommender;
using WindowRecommender.Data;
using WindowRecommender.Data.Fakes;
using WindowRecommender.Fakes;
using WindowRecommender.Native.Fakes;

namespace WindowRecommenderTests
{
    [TestClass]
    public class WindowRecorderTest
    {
        [TestMethod]
        public void TestFocus()
        {
            using (ShimsContext.Create())
            {
                ShimNativeMethods.GetProcessNameIntPtr = windowHandle => "test_process";
                var called = false;
                EventHandler<IntPtr> onFocusHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowFocusedAddEventHandlerOfIntPtr = handler => onFocusHandler = handler
                };
                var windowStack = new ShimWindowStack
                {
                    GetZIndexIntPtr = windowHandle => 1
                };
                ShimQueries.SaveEventIntPtrStringEventNameInt32DoubleInt32 = (window, processName, eventName, rank, score, zIndex) =>
                {
                    called = true;
                    Assert.AreEqual(new IntPtr(2), window);
                    Assert.AreEqual(EventName.Focus, eventName);
                    Assert.AreEqual("test_process", processName);
                    Assert.AreEqual(1, rank);
                    Assert.AreEqual(0.8, score);
                    Assert.AreEqual(1, zIndex);
                };
                var wr = new WindowRecorder(modelEvents, windowStack);
                wr.SetScores(new Dictionary<IntPtr, double>
                {
                    {new IntPtr(1), 1},
                    {new IntPtr(2), 0.8},
                    {new IntPtr(3), 0.7}
                }, new List<IntPtr> { new IntPtr(1), new IntPtr(2), new IntPtr(3) });
                onFocusHandler.Invoke(modelEvents, new IntPtr(2));
                Assert.IsTrue(called);
            }
        }

        [TestMethod]
        public void TestOpen()
        {
            using (ShimsContext.Create())
            {
                ShimNativeMethods.GetProcessNameIntPtr = windowHandle => "test_process";
                var called = false;
                EventHandler<IntPtr> onFocusHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowFocusedAddEventHandlerOfIntPtr = handler => onFocusHandler = handler
                };
                ShimQueries.SaveEventIntPtrStringEventNameInt32DoubleInt32 = (window, processName, eventName, rank, score, zIndex) =>
                {
                    called = true;
                    Assert.AreEqual(new IntPtr(1), window);
                    Assert.AreEqual(EventName.Open, eventName);
                    Assert.AreEqual("test_process", processName);
                    Assert.AreEqual(-1, rank);
                    Assert.AreEqual(-1, score);
                    Assert.AreEqual(-1, zIndex);
                };
                var wr = new WindowRecorder(modelEvents, new WindowStack(modelEvents));
                wr.SetScores(new Dictionary<IntPtr, double>(), new List<IntPtr>());
                onFocusHandler.Invoke(modelEvents, new IntPtr(1));
                Assert.IsTrue(called);
            }
        }

        [TestMethod]
        public void TestClose()
        {
            using (ShimsContext.Create())
            {
                ShimNativeMethods.GetProcessNameIntPtr = windowHandle => "test_process";
                var called = false;
                EventHandler<IntPtr> onClosedHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowClosedAddEventHandlerOfIntPtr = handler => onClosedHandler = handler
                };
                ShimQueries.SaveEventIntPtrStringEventNameInt32DoubleInt32 = (window, processName, eventName, rank, score, zIndex) =>
                {
                    called = true;
                    Assert.AreEqual(new IntPtr(2), window);
                    Assert.AreEqual(EventName.Close, eventName);
                    Assert.AreEqual("test_process", processName);
                    Assert.AreEqual(1, rank);
                    Assert.AreEqual(0.8, score);
                    Assert.AreEqual(-1, zIndex);
                };
                var wr = new WindowRecorder(modelEvents, new WindowStack(modelEvents));
                wr.SetScores(new Dictionary<IntPtr, double>
                {
                    {new IntPtr(1), 1},
                    {new IntPtr(2), 0.8},
                    {new IntPtr(3), 0.7}
                }, new List<IntPtr> { new IntPtr(1), new IntPtr(2), new IntPtr(3) });


                onClosedHandler.Invoke(modelEvents, new IntPtr(4));
                Assert.IsFalse(called);

                onClosedHandler.Invoke(modelEvents, new IntPtr(2));
                Assert.IsTrue(called);
            }
        }
    }
}
