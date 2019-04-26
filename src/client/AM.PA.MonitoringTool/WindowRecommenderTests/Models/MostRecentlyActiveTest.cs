using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using WindowRecommender;
using WindowRecommender.Fakes;
using WindowRecommender.Models;

namespace WindowRecommenderTests
{
    [TestClass]
    public class MostRecentlyActiveTest
    {
        [TestMethod]
        public void TestEmpty()
        {
            var windowEvents = new StubIWindowEvents();
            var mra = new MostRecentlyActive(windowEvents);
            windowEvents.SetupEvent.Invoke(windowEvents, new List<WindowRecord>());
            Assert.AreEqual(0, mra.GetScores().Count);
        }

        [TestMethod]
        public void TestDefault()
        {
            // Add Settings.NumberOfWindows plus one
            var windowEvents = new StubIWindowEvents();
            var mra = new MostRecentlyActive(windowEvents);
            var windowList = new List<WindowRecord>();
            for (var i = 0; i <= Settings.NumberOfWindows; i++)
            {
                windowList.Add(new WindowRecord(new IntPtr(i)));
            }
            windowEvents.SetupEvent.Invoke(windowEvents, windowList);

            var scores = mra.GetScores();
            Assert.AreEqual(Settings.NumberOfWindows + 1, scores.Count);
            for (var i = 0; i < Settings.NumberOfWindows; i++)
            {
                Assert.AreEqual(1, scores[new IntPtr(i)]);
            }
            Assert.AreEqual(0, scores[new IntPtr(Settings.NumberOfWindows)]);
        }

        [TestMethod]
        public void TestOpenedEvent()
        {
            // Add Settings.NumberOfWindows windows
            var windowEvents = new StubIWindowEvents();
            var mra = new MostRecentlyActive(windowEvents);
            var windowList = new List<WindowRecord>();
            for (var i = 0; i < Settings.NumberOfWindows; i++)
            {
                windowList.Add(new WindowRecord(new IntPtr(i)));
            }
            windowEvents.SetupEvent.Invoke(windowEvents, windowList);

            var changed = false;
            mra.OrderChanged += (sender, args) => changed = true;

            // Focus on new window
            windowEvents.WindowOpenedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(Settings.NumberOfWindows)));
            Assert.IsTrue(changed);

            // Assert new window and first two have score of 1
            var scores = mra.GetScores();
            Assert.AreEqual(Settings.NumberOfWindows + 1, scores.Count);
            for (var i = 0; i < Settings.NumberOfWindows - 1; i++)
            {
                Assert.AreEqual(1, scores[new IntPtr(i)]);
            }
            Assert.AreEqual(1, scores[new IntPtr(Settings.NumberOfWindows)]);
        }

        [TestMethod]
        public void TestFocusedEvent()
        {
            // Add Settings.NumberOfWindows plus one
            var windowEvents = new StubIWindowEvents();
            var mra = new MostRecentlyActive(windowEvents);
            var windowList = new List<WindowRecord>();
            for (var i = 0; i < Settings.NumberOfWindows + 1; i++)
            {
                windowList.Add(new WindowRecord(new IntPtr(i)));
            }
            windowEvents.SetupEvent.Invoke(windowEvents, windowList);

            var changed = false;
            mra.OrderChanged += (sender, args) => changed = true;

            // Focus on second window
            windowEvents.WindowFocusedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(1)));
            Assert.IsFalse(changed);

            // Focus on last window
            windowEvents.WindowFocusedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(Settings.NumberOfWindows)));
            Assert.IsTrue(changed);

            // Assert last window and first two have score of 1
            var scores = mra.GetScores();
            Assert.AreEqual(Settings.NumberOfWindows + 1, scores.Count);
            for (var i = 0; i < Settings.NumberOfWindows - 1; i++)
            {
                Assert.AreEqual(1, scores[new IntPtr(i)]);
            }
            Assert.AreEqual(1, scores[new IntPtr(Settings.NumberOfWindows)]);
        }

        [TestMethod]
        public void TestFocusedNewWindowEvent()
        {
            // Add Settings.NumberOfWindows windows
            var windowEvents = new StubIWindowEvents();
            var mra = new MostRecentlyActive(windowEvents);
            var windowList = new List<WindowRecord>();
            for (var i = 0; i < Settings.NumberOfWindows; i++)
            {
                windowList.Add(new WindowRecord(new IntPtr(i)));
            }
            windowEvents.SetupEvent.Invoke(windowEvents, windowList);

            var changed = false;
            mra.OrderChanged += (sender, args) => changed = true;

            // Focus on new window
            windowEvents.WindowFocusedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(Settings.NumberOfWindows)));
            Assert.IsTrue(changed);

            // Assert new window and first two have score of 1
            var scores = mra.GetScores();
            Assert.AreEqual(Settings.NumberOfWindows + 1, scores.Count);
            for (var i = 0; i < Settings.NumberOfWindows - 1; i++)
            {
                Assert.AreEqual(1, scores[new IntPtr(i)]);
            }
            Assert.AreEqual(1, scores[new IntPtr(Settings.NumberOfWindows)]);
        }

        [TestMethod]
        public void TestClosedEvent()
        {
            // Add Settings.NumberOfWindows plus two
            var windowEvents = new StubIWindowEvents();
            var mra = new MostRecentlyActive(windowEvents);
            var windowList = new List<WindowRecord>();
            for (var i = 0; i < Settings.NumberOfWindows + 2; i++)
            {
                windowList.Add(new WindowRecord(new IntPtr(i)));
            }
            windowEvents.SetupEvent.Invoke(windowEvents, windowList);

            var changed = false;
            mra.OrderChanged += (sender, args) => changed = true;

            // Remove last window
            windowEvents.WindowClosedOrMinimizedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(Settings.NumberOfWindows + 1)));
            Assert.IsFalse(changed);

            // Remove first window
            windowEvents.WindowClosedOrMinimizedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(0)));
            Assert.IsTrue(changed);

            // Assert remaining windows have a score of 1
            var scores = mra.GetScores();
            Assert.AreEqual(Settings.NumberOfWindows, scores.Count);
            for (var i = 1; i < Settings.NumberOfWindows + 1; i++)
            {
                Assert.AreEqual(1, scores[new IntPtr(i)]);
            }
        }
    }
}
