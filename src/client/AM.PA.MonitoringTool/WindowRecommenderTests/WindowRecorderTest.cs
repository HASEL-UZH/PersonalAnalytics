using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using WindowRecommender;
using WindowRecommender.Data;
using WindowRecommender.Data.Fakes;
using WindowRecommender.Fakes;

namespace WindowRecommenderTests
{
    [TestClass]
    public class WindowRecorderTest
    {
        [TestMethod]
        public void TestOpen()
        {
            using (ShimsContext.Create())
            {
                var called = false;
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
                var windowEvents = new StubIWindowEvents();
                // ReSharper disable once ObjectCreationAsStatement
                new WindowRecorder(windowEvents, new WindowStack(windowEvents));
                windowEvents.WindowOpenedEvent(windowEvents, new WindowRecord(new IntPtr(1), "", "test_process"));
                Assert.IsTrue(called);
            }
        }

        [TestMethod]
        public void TestFocus()
        {
            using (ShimsContext.Create())
            {
                var called = false;
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
                var windowEvents = new StubIWindowEvents();
                var windowStack = new ShimWindowStack
                {
                    GetZIndexIntPtr = windowHandle => 1
                };
                var wr = new WindowRecorder(windowEvents, windowStack);
                wr.SetScores(new Dictionary<IntPtr, double>
                {
                    {new IntPtr(1), 1},
                    {new IntPtr(2), 0.8},
                    {new IntPtr(3), 0.7}
                }, new List<IntPtr> { new IntPtr(1), new IntPtr(2), new IntPtr(3) });
                windowEvents.WindowFocusedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(2), "", "test_process"));
                Assert.IsTrue(called);
            }
        }

        [TestMethod]
        public void TestFocus_Open()
        {
            using (ShimsContext.Create())
            {
                var called = false;
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
                var windowEvents = new StubIWindowEvents();
                // ReSharper disable once ObjectCreationAsStatement
                new WindowRecorder(windowEvents, new WindowStack(windowEvents));
                windowEvents.WindowFocusedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(1), "", "test_process"));
                Assert.IsTrue(called);
            }
        }

        [TestMethod]
        public void TestClose()
        {
            using (ShimsContext.Create())
            {
                var called = false;
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
                var windowEvents = new StubIWindowEvents();
                var wr = new WindowRecorder(windowEvents, new WindowStack(windowEvents));
                wr.SetScores(new Dictionary<IntPtr, double>
                {
                    {new IntPtr(1), 1},
                    {new IntPtr(2), 0.8},
                    {new IntPtr(3), 0.7}
                }, new List<IntPtr> { new IntPtr(1), new IntPtr(2), new IntPtr(3) });

                windowEvents.WindowClosedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(4), "", "test_process"));
                Assert.IsFalse(called);

                windowEvents.WindowClosedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(2), "", "test_process"));
                Assert.IsTrue(called);
            }
        }

        [TestMethod]
        public void TestMinimize()
        {
            using (ShimsContext.Create())
            {
                var called = false;
                ShimQueries.SaveEventIntPtrStringEventNameInt32DoubleInt32 = (window, processName, eventName, rank, score, zIndex) =>
                {
                    called = true;
                    Assert.AreEqual(new IntPtr(2), window);
                    Assert.AreEqual(EventName.Minimize, eventName);
                    Assert.AreEqual("test_process", processName);
                    Assert.AreEqual(1, rank);
                    Assert.AreEqual(0.8, score);
                    Assert.AreEqual(-1, zIndex);
                };
                var windowEvents = new StubIWindowEvents();
                var wr = new WindowRecorder(windowEvents, new WindowStack(windowEvents));
                wr.SetScores(new Dictionary<IntPtr, double>
                {
                    {new IntPtr(1), 1},
                    {new IntPtr(2), 0.8},
                    {new IntPtr(3), 0.7}
                }, new List<IntPtr> { new IntPtr(1), new IntPtr(2), new IntPtr(3) });

                windowEvents.WindowMinimizedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(4), "", "test_process"));
                Assert.IsFalse(called);

                windowEvents.WindowMinimizedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(2), "", "test_process"));
                Assert.IsTrue(called);
            }
        }
    }
}
