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
    public class FrequencyTest
    {
        /// <summary>
        /// Delta for comparing double values
        /// </summary>
        private const double Delta = 0.000001;

        /// <summary>
        /// Step value for events within one interval
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
            Assert.IsTrue(Settings.FrequencyIntervalSeconds > IntervalStep * 2);
        }

        [TestMethod]
        public void TestOnInterval_SetWindow()
        {
            using (ShimsContext.Create())
            {
                var setWindowsTime = DateTime.Parse(Timestamp);
                var intervalTime = setWindowsTime.AddSeconds(Settings.FrequencyIntervalSeconds);

                var called = false;
                var frequency = new Frequency(new ModelEvents());
                frequency.OrderChanged += (sender, args) => called = true;
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), frequency.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                frequency.SetWindows(new List<IntPtr> { new IntPtr(1) });

                System.Fakes.ShimDateTime.NowGet = () => intervalTime;
                frequency.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    { new IntPtr(1), 1 }
                }, frequency.GetScores());
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
                var intervalTime = setWindowsTime.AddSeconds(Settings.FrequencyIntervalSeconds);

                EventHandler<IntPtr> focusHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowFocusedAddEventHandlerOfIntPtr = handler => focusHandler = handler
                };
                var frequency = new Frequency(modelEvents);
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), frequency.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                frequency.SetWindows(new List<IntPtr> { new IntPtr(1) });

                System.Fakes.ShimDateTime.NowGet = () => focusEventTime;
                focusHandler.Invoke(modelEvents, new IntPtr(2));

                System.Fakes.ShimDateTime.NowGet = () => intervalTime;
                frequency.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    { new IntPtr(1), 0.5 },
                    { new IntPtr(2), 0.5 }
                }, frequency.GetScores());
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
                var intervalTime = setWindowsTime.AddSeconds(Settings.FrequencyIntervalSeconds);

                EventHandler<IntPtr> focusHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowFocusedAddEventHandlerOfIntPtr = handler => focusHandler = handler
                };
                var frequency = new Frequency(modelEvents);
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), frequency.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                frequency.SetWindows(new List<IntPtr> { new IntPtr(1) });

                System.Fakes.ShimDateTime.NowGet = () => firstFocusEventTime;
                focusHandler.Invoke(modelEvents, new IntPtr(2));

                System.Fakes.ShimDateTime.NowGet = () => secondFocusEventTime;
                focusHandler.Invoke(modelEvents, new IntPtr(1));

                System.Fakes.ShimDateTime.NowGet = () => intervalTime;
                frequency.OnInterval(null, null);

                var scores = frequency.GetScores();
                Assert.AreEqual(2, scores.Count);
                Assert.AreEqual(2D / 3D, scores[new IntPtr(1)], Delta);
                Assert.AreEqual(1D / 3D, scores[new IntPtr(2)], Delta);
            }
        }

        [TestMethod]
        public void TestOnInterval_SetWindow_MultipleInterval()
        {
            using (ShimsContext.Create())
            {
                var setWindowsTime = DateTime.Parse(Timestamp);
                var firstIntervalTime = setWindowsTime.AddSeconds(Settings.FrequencyIntervalSeconds);
                var secondIntervalTime = firstIntervalTime.AddSeconds(Settings.FrequencyIntervalSeconds);

                var called = false;
                var frequency = new Frequency(new ModelEvents());
                frequency.OrderChanged += (sender, args) => called = true;
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), frequency.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                frequency.SetWindows(new List<IntPtr> { new IntPtr(1) });

                System.Fakes.ShimDateTime.NowGet = () => firstIntervalTime;
                frequency.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    { new IntPtr(1), 1 }
                }, frequency.GetScores());
                Assert.IsTrue(called);
                called = false;

                System.Fakes.ShimDateTime.NowGet = () => secondIntervalTime;
                frequency.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    { new IntPtr(1), 1 }
                }, frequency.GetScores());
                Assert.IsFalse(called);
            }
        }

        [TestMethod]
        public void TestOnInterval_SetWindow_MultipleInterval_Focus()
        {
            using (ShimsContext.Create())
            {
                var setWindowsTime = DateTime.Parse(Timestamp);
                var firstIntervalTime = setWindowsTime.AddSeconds(Settings.FrequencyIntervalSeconds);
                var focusTime = firstIntervalTime.AddSeconds(IntervalStep);
                var secondIntervalTime = firstIntervalTime.AddSeconds(Settings.FrequencyIntervalSeconds);

                var called = false;
                EventHandler<IntPtr> focusHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowFocusedAddEventHandlerOfIntPtr = handler => focusHandler = handler
                };
                var frequency = new Frequency(modelEvents);
                frequency.OrderChanged += (sender, args) => called = true;
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), frequency.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                frequency.SetWindows(new List<IntPtr> { new IntPtr(1) });

                System.Fakes.ShimDateTime.NowGet = () => firstIntervalTime;
                frequency.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    { new IntPtr(1), 1 }
                }, frequency.GetScores());
                Assert.IsTrue(called);
                called = false;

                System.Fakes.ShimDateTime.NowGet = () => focusTime;
                focusHandler.Invoke(modelEvents, new IntPtr(2));

                System.Fakes.ShimDateTime.NowGet = () => secondIntervalTime;
                frequency.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    { new IntPtr(1), 0.5 },
                    { new IntPtr(2), 0.5 }
                }, frequency.GetScores());
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
                var intervalTime = setWindowsTime.AddSeconds(Settings.FrequencyIntervalSeconds);
                var frequencyTime = intervalTime.AddMinutes(Settings.FrequencyTimeframeMinutes);

                var called = false;
                var frequency = new Frequency(new ModelEvents());
                frequency.OrderChanged += (sender, args) => called = true;
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), frequency.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                frequency.SetWindows(new List<IntPtr> { new IntPtr(1) });

                System.Fakes.ShimDateTime.NowGet = () => intervalTime;
                frequency.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    { new IntPtr(1), 1 }
                }, frequency.GetScores());
                Assert.IsTrue(called);
                called = false;

                var nextIntervalTime = intervalTime.AddSeconds(Settings.FrequencyIntervalSeconds);
                while (nextIntervalTime < frequencyTime)
                {
                    var thisIntervalTime = nextIntervalTime;
                    System.Fakes.ShimDateTime.NowGet = () => thisIntervalTime;
                    frequency.OnInterval(null, null);
                    Assert.IsFalse(called, setWindowsTime + " - " + intervalTime + " - " + thisIntervalTime + " - " + frequencyTime);
                    nextIntervalTime = nextIntervalTime.AddSeconds(Settings.FrequencyIntervalSeconds);
                }

                System.Fakes.ShimDateTime.NowGet = () => frequencyTime;
                frequency.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), frequency.GetScores());
                Assert.IsTrue(called);
            }
        }

        [TestMethod]
        public void TestOnInterval_Closed_New()
        {
            using (ShimsContext.Create())
            {
                var setWindowsTime = DateTime.Parse(Timestamp);
                var focusEventTime = setWindowsTime.AddSeconds(IntervalStep);
                var closedEventTime = focusEventTime.AddSeconds(IntervalStep);
                var intervalTime = setWindowsTime.AddSeconds(Settings.FrequencyIntervalSeconds);

                EventHandler<IntPtr> focusHandler = null;
                EventHandler<IntPtr> closedHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowFocusedAddEventHandlerOfIntPtr = handler => focusHandler = handler,
                    WindowClosedAddEventHandlerOfIntPtr = handler => closedHandler = handler
                };
                var frequency = new Frequency(modelEvents);
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), frequency.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                frequency.SetWindows(new List<IntPtr> { new IntPtr(1) });

                System.Fakes.ShimDateTime.NowGet = () => focusEventTime;
                focusHandler.Invoke(modelEvents, new IntPtr(2));

                System.Fakes.ShimDateTime.NowGet = () => closedEventTime;
                closedHandler.Invoke(modelEvents, new IntPtr(2));

                System.Fakes.ShimDateTime.NowGet = () => intervalTime;
                frequency.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    {new IntPtr(1), 1}
                }, frequency.GetScores());
            }
        }

        [TestMethod]
        public void TestOnInterval_Closed()
        {
            using (ShimsContext.Create())
            {
                var setWindowsTime = DateTime.Parse(Timestamp);
                var focusEventTime = setWindowsTime.AddSeconds(IntervalStep);
                var firstIntervalTime = setWindowsTime.AddSeconds(Settings.FrequencyIntervalSeconds);
                var closedEventTime = firstIntervalTime.AddSeconds(IntervalStep);
                var secondIntervalTime = firstIntervalTime.AddSeconds(Settings.FrequencyIntervalSeconds);

                EventHandler<IntPtr> focusHandler = null;
                EventHandler<IntPtr> closedHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowFocusedAddEventHandlerOfIntPtr = handler => focusHandler = handler,
                    WindowClosedAddEventHandlerOfIntPtr = handler => closedHandler = handler
                };
                var frequency = new Frequency(modelEvents);
                var called = false;
                frequency.OrderChanged += (sender, args) => called = true;
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), frequency.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                frequency.SetWindows(new List<IntPtr> { new IntPtr(1) });

                System.Fakes.ShimDateTime.NowGet = () => focusEventTime;
                focusHandler.Invoke(modelEvents, new IntPtr(2));

                System.Fakes.ShimDateTime.NowGet = () => firstIntervalTime;
                frequency.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    { new IntPtr(1), 0.5 },
                    { new IntPtr(2), 0.5 }
                }, frequency.GetScores());
                Assert.IsTrue(called);
                called = false;

                System.Fakes.ShimDateTime.NowGet = () => closedEventTime;
                closedHandler.Invoke(modelEvents, new IntPtr(2));

                System.Fakes.ShimDateTime.NowGet = () => secondIntervalTime;
                frequency.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    {new IntPtr(1), 1}
                }, frequency.GetScores());
                Assert.IsTrue(called);
            }
        }
    }
}
