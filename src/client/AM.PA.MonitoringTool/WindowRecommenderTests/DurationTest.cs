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
    public class DurationTest
    {
        /// <summary>
        /// Delta for comparing double values
        /// </summary>
        private const double Delta = 0.000001;

        /// <summary>
        /// Step set to a value that gives nice results on division by 60 in most cases
        /// </summary>
        private const int IntervalStep = 3;

        /// <summary>
        /// Starting timestamp for test
        /// </summary>
        private const string Timestamp = "2002-02-02T14:14:14.0000000Z";

        [TestMethod]
        public void IntervalSeconds()
        {
            // Dummy test to assure an interval is larger than two steps
            Assert.IsTrue(Settings.DurationIntervalSeconds > IntervalStep * 2);
        }

        [TestMethod]
        public void TestOnInterval_SetWindow()
        {
            using (ShimsContext.Create())
            {
                var setWindowsTime = DateTime.Parse(Timestamp);
                var intervalTime = setWindowsTime.AddSeconds(Settings.DurationIntervalSeconds);

                var called = false;
                var duration = new Duration(new ModelEvents());
                duration.OrderChanged += (sender, args) => called = true;
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), duration.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                duration.SetWindows(new List<IntPtr> { new IntPtr(1) });

                System.Fakes.ShimDateTime.NowGet = () => intervalTime;
                duration.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    { new IntPtr(1), Settings.DurationIntervalSeconds / (Settings.DurationTimeframeMinutes * 60D) }
                }, duration.GetScores());
                Assert.IsTrue(called);
            }
        }

        [TestMethod]
        public void TestOnInterval_SetWindow_Focus()
        {
            using (ShimsContext.Create())
            {
                var setWindowsTime = DateTime.Parse(Timestamp);
                var focusEventTime = setWindowsTime.AddSeconds(IntervalStep);
                var intervalTime = setWindowsTime.AddSeconds(Settings.DurationIntervalSeconds);

                EventHandler<IntPtr> focusHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowFocusedAddEventHandlerOfIntPtr = handler => focusHandler = handler
                };
                var duration = new Duration(modelEvents);
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), duration.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                duration.SetWindows(new List<IntPtr> { new IntPtr(1) });

                System.Fakes.ShimDateTime.NowGet = () => focusEventTime;
                focusHandler.Invoke(modelEvents, new IntPtr(2));

                System.Fakes.ShimDateTime.NowGet = () => intervalTime;
                duration.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    { new IntPtr(1), IntervalStep / (Settings.DurationTimeframeMinutes * 60D) },
                    { new IntPtr(2), (Settings.DurationIntervalSeconds - IntervalStep) / (Settings.DurationTimeframeMinutes * 60D) }
                }, duration.GetScores());
            }
        }

        [TestMethod]
        public void TestOnInterval_SetWindow_Focus_Focus()
        {
            using (ShimsContext.Create())
            {
                var setWindowsTime = DateTime.Parse(Timestamp);
                var firstFocusEventTime = setWindowsTime.AddSeconds(IntervalStep);
                var secondFocusEventTime = firstFocusEventTime.AddSeconds(IntervalStep);
                var intervalTime = setWindowsTime.AddSeconds(Settings.DurationIntervalSeconds);

                EventHandler<IntPtr> focusHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowFocusedAddEventHandlerOfIntPtr = handler => focusHandler = handler
                };
                var duration = new Duration(modelEvents);
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), duration.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                duration.SetWindows(new List<IntPtr> { new IntPtr(1) });

                System.Fakes.ShimDateTime.NowGet = () => firstFocusEventTime;
                focusHandler.Invoke(modelEvents, new IntPtr(2));

                System.Fakes.ShimDateTime.NowGet = () => secondFocusEventTime;
                focusHandler.Invoke(modelEvents, new IntPtr(1));

                System.Fakes.ShimDateTime.NowGet = () => intervalTime;
                duration.OnInterval(null, null);

                var scores = duration.GetScores();
                Assert.AreEqual(2, scores.Count);
                Assert.AreEqual((Settings.DurationIntervalSeconds - IntervalStep) / (Settings.DurationTimeframeMinutes * 60D), scores[new IntPtr(1)], Delta);
                Assert.AreEqual(IntervalStep / (Settings.DurationTimeframeMinutes * 60D), scores[new IntPtr(2)], Delta);
            }
        }

        [TestMethod]
        public void TestOnInterval_SetWindow_MultipleInterval()
        {
            using (ShimsContext.Create())
            {
                var setWindowsTime = DateTime.Parse(Timestamp);
                var firstIntervalTime = setWindowsTime.AddSeconds(Settings.DurationIntervalSeconds);
                var secondIntervalTime = firstIntervalTime.AddSeconds(Settings.DurationIntervalSeconds);

                var called = false;
                var duration = new Duration(new ModelEvents());
                duration.OrderChanged += (sender, args) => called = true;
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), duration.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                duration.SetWindows(new List<IntPtr> { new IntPtr(1) });

                System.Fakes.ShimDateTime.NowGet = () => firstIntervalTime;
                duration.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    { new IntPtr(1), Settings.DurationIntervalSeconds / (Settings.DurationTimeframeMinutes * 60D) }
                }, duration.GetScores());
                Assert.IsTrue(called);
                called = false;

                System.Fakes.ShimDateTime.NowGet = () => secondIntervalTime;
                duration.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    { new IntPtr(1), Settings.DurationIntervalSeconds * 2 / (Settings.DurationTimeframeMinutes * 60D) }
                }, duration.GetScores());
                Assert.IsFalse(called);
            }
        }

        [TestMethod]
        public void TestOnInterval_SetWindow_Focus_MultipleInterval()
        {
            using (ShimsContext.Create())
            {
                var setWindowsTime = DateTime.Parse(Timestamp);
                var focusTime = setWindowsTime.AddSeconds(Settings.DurationIntervalSeconds - IntervalStep);
                var firstIntervalTime = setWindowsTime.AddSeconds(Settings.DurationIntervalSeconds);
                var secondIntervalTime = firstIntervalTime.AddSeconds(Settings.DurationIntervalSeconds);

                var called = false;
                EventHandler<IntPtr> focusHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowFocusedAddEventHandlerOfIntPtr = handler => focusHandler = handler
                };
                var duration = new Duration(modelEvents);
                duration.OrderChanged += (sender, args) => called = true;
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), duration.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                duration.SetWindows(new List<IntPtr> { new IntPtr(1) });

                System.Fakes.ShimDateTime.NowGet = () => focusTime;
                focusHandler.Invoke(modelEvents, new IntPtr(2));

                System.Fakes.ShimDateTime.NowGet = () => firstIntervalTime;
                duration.OnInterval(null, null);

                var scores = duration.GetScores();
                Assert.AreEqual(2, scores.Count);
                Assert.AreEqual((Settings.DurationIntervalSeconds - IntervalStep) / (Settings.DurationTimeframeMinutes * 60D), scores[new IntPtr(1)], Delta);
                Assert.AreEqual(IntervalStep / (Settings.DurationTimeframeMinutes * 60D), scores[new IntPtr(2)], Delta);
                Assert.IsTrue(called);
                called = false;

                System.Fakes.ShimDateTime.NowGet = () => secondIntervalTime;
                duration.OnInterval(null, null);

                scores = duration.GetScores();
                Assert.AreEqual(2, scores.Count);
                Assert.AreEqual((Settings.DurationIntervalSeconds - IntervalStep) / (Settings.DurationTimeframeMinutes * 60D), scores[new IntPtr(1)], Delta);
                Assert.AreEqual((Settings.DurationIntervalSeconds + IntervalStep) / (Settings.DurationTimeframeMinutes * 60D), scores[new IntPtr(2)], Delta);
                Assert.IsTrue(called);
            }
        }

        [TestMethod]
        public void TestOnInterval_Remove()
        {
            // Start with window 1, focus on 2 within one interval
            // Continue until window 1 leaves the timeframe
            using (ShimsContext.Create())
            {
                var setWindowsTime = DateTime.Parse(Timestamp);
                var focusTime = setWindowsTime.AddSeconds(IntervalStep);
                var intervalTime = setWindowsTime.AddSeconds(Settings.DurationIntervalSeconds);
                var durationTime = intervalTime.AddMinutes(Settings.DurationTimeframeMinutes);

                EventHandler<IntPtr> focusHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowFocusedAddEventHandlerOfIntPtr = handler => focusHandler = handler
                };
                var duration = new Duration(modelEvents);
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), duration.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                duration.SetWindows(new List<IntPtr> { new IntPtr(1) });

                System.Fakes.ShimDateTime.NowGet = () => focusTime;
                focusHandler.Invoke(modelEvents, new IntPtr(2));

                System.Fakes.ShimDateTime.NowGet = () => intervalTime;
                duration.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    { new IntPtr(1), IntervalStep / (Settings.DurationTimeframeMinutes * 60D) },
                    { new IntPtr(2), (Settings.DurationIntervalSeconds - IntervalStep) / (Settings.DurationTimeframeMinutes * 60D) }
                }, duration.GetScores());

                var nextIntervalTime = intervalTime.AddSeconds(Settings.DurationIntervalSeconds);
                while (nextIntervalTime <= durationTime)
                {
                    var thisIntervalTime = nextIntervalTime;
                    System.Fakes.ShimDateTime.NowGet = () => thisIntervalTime;
                    duration.OnInterval(null, null);
                    nextIntervalTime = nextIntervalTime.AddSeconds(Settings.DurationIntervalSeconds);
                }

                System.Fakes.ShimDateTime.NowGet = () => durationTime;
                duration.OnInterval(null, null);

                var scores = duration.GetScores();
                Assert.AreEqual(1, scores.Count);
                Assert.AreEqual(1, scores[new IntPtr(2)], Delta);
            }
        }

        [TestMethod]
        public void TestOnInterval_Closed()
        {
            using (ShimsContext.Create())
            {
                var setWindowsTime = DateTime.Parse(Timestamp);
                var focusEventTime = setWindowsTime.AddSeconds(IntervalStep);
                var interval1Time = setWindowsTime.AddSeconds(Settings.DurationIntervalSeconds);
                var closedEventTime = interval1Time.AddSeconds(IntervalStep);
                var interval2Time = interval1Time.AddSeconds(Settings.DurationIntervalSeconds);

                EventHandler<IntPtr> focusHandler = null;
                EventHandler<IntPtr> closedHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowFocusedAddEventHandlerOfIntPtr = handler => focusHandler = handler,
                    WindowClosedAddEventHandlerOfIntPtr = handler => closedHandler = handler
                };
                var duration = new Duration(modelEvents);
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), duration.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                duration.SetWindows(new List<IntPtr> { new IntPtr(1) });

                System.Fakes.ShimDateTime.NowGet = () => focusEventTime;
                focusHandler.Invoke(modelEvents, new IntPtr(2));

                System.Fakes.ShimDateTime.NowGet = () => interval1Time;
                duration.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    { new IntPtr(1), IntervalStep / (Settings.DurationTimeframeMinutes * 60D) },
                    { new IntPtr(2), (Settings.DurationIntervalSeconds - IntervalStep) / (Settings.DurationTimeframeMinutes * 60D) }
                }, duration.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => closedEventTime;
                closedHandler.Invoke(modelEvents, new IntPtr(2));
                focusHandler.Invoke(modelEvents, new IntPtr(1));

                System.Fakes.ShimDateTime.NowGet = () => interval2Time;
                duration.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    {new IntPtr(1), Settings.DurationIntervalSeconds / (Settings.DurationTimeframeMinutes * 60D)}
                }, duration.GetScores());
            }
        }

        [TestMethod]
        public void TestOnInterval_New_Closed()
        {
            using (ShimsContext.Create())
            {
                var setWindowsTime = DateTime.Parse(Timestamp);
                var focusEventTime = setWindowsTime.AddSeconds(IntervalStep);
                var closedEventTime = focusEventTime.AddSeconds(IntervalStep);
                var intervalTime = setWindowsTime.AddSeconds(Settings.DurationIntervalSeconds);

                EventHandler<IntPtr> focusHandler = null;
                EventHandler<IntPtr> closedHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowFocusedAddEventHandlerOfIntPtr = handler => focusHandler = handler,
                    WindowClosedAddEventHandlerOfIntPtr = handler => closedHandler = handler
                };
                var duration = new Duration(modelEvents);
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), duration.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                duration.SetWindows(new List<IntPtr> { new IntPtr(1) });

                System.Fakes.ShimDateTime.NowGet = () => focusEventTime;
                focusHandler.Invoke(modelEvents, new IntPtr(2));

                System.Fakes.ShimDateTime.NowGet = () => closedEventTime;
                closedHandler.Invoke(modelEvents, new IntPtr(2));
                focusHandler.Invoke(modelEvents, new IntPtr(1));

                System.Fakes.ShimDateTime.NowGet = () => intervalTime;
                duration.OnInterval(null, null);

                var scores = duration.GetScores();
                Assert.AreEqual(1, scores.Count);
                Assert.AreEqual((Settings.DurationIntervalSeconds - IntervalStep) / (Settings.DurationTimeframeMinutes * 60D), scores[new IntPtr(1)], Delta);
            }
        }
    }
}
