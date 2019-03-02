using Microsoft.QualityTools.Testing.Fakes;
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
            var mra = new MostRecentlyActive(new ModelEvents());
            mra.SetWindows(new List<IntPtr>());
            Assert.AreEqual(0, mra.GetScores().Count);
        }

        [TestMethod]
        public void TestDefault()
        {
            // Add Settings.NumberOfWindows plus one
            var mra = new MostRecentlyActive(new ModelEvents());
            var windowList = new List<IntPtr>();
            for (var i = 0; i <= Settings.NumberOfWindows; i++)
            {
                windowList.Add(new IntPtr(i));
            }
            mra.SetWindows(windowList);

            var scores = mra.GetScores();
            Assert.AreEqual(Settings.NumberOfWindows + 1, scores.Count);
            for (var i = 0; i < Settings.NumberOfWindows; i++)
            {
                Assert.AreEqual(1, scores[new IntPtr(i)]);
            }
            Assert.AreEqual(0, scores[new IntPtr(Settings.NumberOfWindows)]);
        }

        [TestMethod]
        public void TestFocusedEvent()
        {
            using (ShimsContext.Create())
            {
                EventHandler<IntPtr> onFocusHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowFocusedAddEventHandlerOfIntPtr = handler => onFocusHandler = handler
                };

                // Add Settings.NumberOfWindows plus one
                var mra = new MostRecentlyActive(modelEvents);
                var windowList = new List<IntPtr>();
                for (var i = 0; i <= Settings.NumberOfWindows; i++)
                {
                    windowList.Add(new IntPtr(i));
                }
                mra.SetWindows(windowList);

                // Focus on last window
                onFocusHandler.Invoke(modelEvents, new IntPtr(Settings.NumberOfWindows));

                // Assert last window and first two have score of 1
                var scores = mra.GetScores();
                Assert.AreEqual(Settings.NumberOfWindows + 1, scores.Count);
                for (var i = 0; i < Settings.NumberOfWindows - 1; i++)
                {
                    Assert.AreEqual(1, scores[new IntPtr(i)]);
                }
                Assert.AreEqual(1, scores[new IntPtr(Settings.NumberOfWindows)]);
            }
        }

        [TestMethod]
        public void TestFocusedNewWindowEvent()
        {
            using (ShimsContext.Create())
            {
                EventHandler<IntPtr> onFocusHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowFocusedAddEventHandlerOfIntPtr = handler => onFocusHandler = handler
                };

                // Add Settings.NumberOfWindows windows
                var mra = new MostRecentlyActive(modelEvents);
                var windowList = new List<IntPtr>();
                for (var i = 0; i < Settings.NumberOfWindows; i++)
                {
                    windowList.Add(new IntPtr(i));
                }
                mra.SetWindows(windowList);

                // Focus on new window
                onFocusHandler.Invoke(modelEvents, new IntPtr(Settings.NumberOfWindows));

                // Assert new window and first two have score of 1
                var scores = mra.GetScores();
                Assert.AreEqual(Settings.NumberOfWindows + 1, scores.Count);
                for (var i = 0; i < Settings.NumberOfWindows - 1; i++)
                {
                    Assert.AreEqual(1, scores[new IntPtr(i)]);
                }
                Assert.AreEqual(1, scores[new IntPtr(Settings.NumberOfWindows)]);
            }
        }

        [TestMethod]
        public void TestFocusedEvent_OrderChanged()
        {
            using (ShimsContext.Create())
            {
                EventHandler<IntPtr> onFocusHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowFocusedAddEventHandlerOfIntPtr = handler => onFocusHandler = handler
                };

                // Add Settings.NumberOfWindows plus one
                var mra = new MostRecentlyActive(modelEvents);
                var windowList = new List<IntPtr>();
                for (var i = 0; i < Settings.NumberOfWindows + 1; i++)
                {
                    windowList.Add(new IntPtr(i));
                }
                mra.SetWindows(windowList);

                var changed = false;
                mra.OrderChanged += (sender, args) => changed = true;

                // Focus on first window and expect order not to change
                onFocusHandler.Invoke(modelEvents, new IntPtr(0));
                Assert.IsFalse(changed);

                // Focus on second window and expect order not to change
                onFocusHandler.Invoke(modelEvents, new IntPtr(1));
                Assert.IsFalse(changed);

                // Focus on last window and expect order to change
                onFocusHandler.Invoke(modelEvents, new IntPtr(Settings.NumberOfWindows));
                Assert.IsTrue(changed);
            }
        }

        [TestMethod]
        public void TestClosedEvent()
        {
            using (ShimsContext.Create())
            {
                EventHandler<IntPtr> onCloseHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowClosedAddEventHandlerOfIntPtr = handler => onCloseHandler = handler
                };

                // Add Settings.NumberOfWindows plus one
                var mra = new MostRecentlyActive(modelEvents);
                var windowList = new List<IntPtr>();
                for (var i = 0; i <= Settings.NumberOfWindows; i++)
                {
                    windowList.Add(new IntPtr(i));
                }
                mra.SetWindows(windowList);

                // Remove first window
                onCloseHandler.Invoke(modelEvents, new IntPtr(0));

                // Assert remaining windows have a score of 1
                var scores = mra.GetScores();
                Assert.AreEqual(Settings.NumberOfWindows, scores.Count);
                for (var i = 1; i <= Settings.NumberOfWindows; i++)
                {
                    Assert.AreEqual(1, scores[new IntPtr(i)]);
                }
            }
        }

        [TestMethod]
        public void TestClosedEvent_OrderChanged()
        {
            using (ShimsContext.Create())
            {
                EventHandler<IntPtr> onCloseHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowClosedAddEventHandlerOfIntPtr = handler => onCloseHandler = handler
                };

                // Add Settings.NumberOfWindows plus two
                var mra = new MostRecentlyActive(modelEvents);
                var windowList = new List<IntPtr>();
                for (var i = 0; i < Settings.NumberOfWindows + 2; i++)
                {
                    windowList.Add(new IntPtr(i));
                }
                mra.SetWindows(windowList);

                var changed = false;
                mra.OrderChanged += (sender, args) => changed = true;

                // Remove first window out of range and expect order not to change
                onCloseHandler.Invoke(modelEvents, new IntPtr(Settings.NumberOfWindows));
                Assert.IsFalse(changed);

                // Remove first window and expect order to change
                onCloseHandler.Invoke(modelEvents, new IntPtr(0));
                Assert.IsTrue(changed);

                // Assert remaining windows have a score of 1
                var scores = mra.GetScores();
                Assert.AreEqual(Settings.NumberOfWindows, scores.Count);
                foreach (var score in scores.Values)
                {
                    Assert.AreEqual(1, score);
                }
            }
        }
    }
}
