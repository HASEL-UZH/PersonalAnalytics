using System;
using System.Collections.Generic;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowRecommender;
using WindowRecommender.Fakes;
using WindowRecommender.Models;
using WindowRecommenderTests.Tools;

namespace WindowRecommenderTests.Models
{
    [TestClass]
    public class FrequencyTest
    {
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
        public void TestEmpty()
        {
            var windowEvents = new StubIWindowEvents();
            var frequency = new Frequency(windowEvents);
            windowEvents.SetupEvent.Invoke(windowEvents, new List<WindowRecord>());
            Assert.AreEqual(0, frequency.GetScores().Count);
        }

        [TestMethod]
        public void TestOnInterval_SetWindow()
        {
            using (ShimsContext.Create())
            {
                var setWindowsTime = DateTime.Parse(Timestamp);
                var intervalTime = setWindowsTime.AddSeconds(Settings.FrequencyIntervalSeconds);

                var called = false;
                var windowEvents = new StubIWindowEvents();
                var frequency = new Frequency(windowEvents);
                frequency.OrderChanged += (sender, args) => called = true;
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), frequency.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                windowEvents.SetupEvent.Invoke(windowEvents, new List<WindowRecord>
                {
                    new WindowRecord(new IntPtr(1))
                });

                System.Fakes.ShimDateTime.NowGet = () => intervalTime;
                frequency.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    { new IntPtr(1), 1 }
                }, frequency.GetScores());
                Assert.IsFalse(called);
            }
        }

        [TestMethod]
        public void TestOnInterval_SetWindow_Open()
        {
            using (ShimsContext.Create())
            {
                var setWindowsTime = DateTime.Parse(Timestamp);
                var openEventTime = setWindowsTime.AddSeconds(IntervalStep);
                var intervalTime = setWindowsTime.AddSeconds(Settings.FrequencyIntervalSeconds);

                var called = false;
                var windowEvents = new StubIWindowEvents();
                var frequency = new Frequency(windowEvents);
                frequency.OrderChanged += (sender, args) => called = true;
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), frequency.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                windowEvents.SetupEvent.Invoke(windowEvents, new List<WindowRecord>
                {
                    new WindowRecord(new IntPtr(1))
                });

                System.Fakes.ShimDateTime.NowGet = () => openEventTime;
                windowEvents.WindowOpenedOrFocusedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(2)));

                System.Fakes.ShimDateTime.NowGet = () => intervalTime;
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
        public void TestOnInterval_SetWindow_Focus()
        {
            using (ShimsContext.Create())
            {
                var setWindowsTime = DateTime.Parse(Timestamp);
                var focusEventTime = setWindowsTime.AddSeconds(IntervalStep);
                var intervalTime = setWindowsTime.AddSeconds(Settings.FrequencyIntervalSeconds);

                var called = false;
                var windowEvents = new StubIWindowEvents();
                var frequency = new Frequency(windowEvents);
                frequency.OrderChanged += (sender, args) => called = true;
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), frequency.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                windowEvents.SetupEvent.Invoke(windowEvents, new List<WindowRecord>
                {
                    new WindowRecord(new IntPtr(1))
                });

                System.Fakes.ShimDateTime.NowGet = () => focusEventTime;
                windowEvents.WindowOpenedOrFocusedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(2)));

                System.Fakes.ShimDateTime.NowGet = () => intervalTime;
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
        public void TestOnInterval_SetWindow_Focus_Focus()
        {
            using (ShimsContext.Create())
            {
                var setWindowsTime = DateTime.Parse(Timestamp);
                var firstFocusEventTime = setWindowsTime.AddSeconds(IntervalStep);
                var secondFocusEventTime = firstFocusEventTime.AddSeconds(IntervalStep);
                var intervalTime = setWindowsTime.AddSeconds(Settings.FrequencyIntervalSeconds);

                var windowEvents = new StubIWindowEvents();
                var frequency = new Frequency(windowEvents);
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), frequency.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                windowEvents.SetupEvent.Invoke(windowEvents, new List<WindowRecord>
                {
                    new WindowRecord(new IntPtr(1))
                });

                System.Fakes.ShimDateTime.NowGet = () => firstFocusEventTime;
                windowEvents.WindowOpenedOrFocusedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(2)));

                System.Fakes.ShimDateTime.NowGet = () => secondFocusEventTime;
                windowEvents.WindowOpenedOrFocusedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(1)));

                System.Fakes.ShimDateTime.NowGet = () => intervalTime;
                frequency.OnInterval(null, null);

                CollectionAssert.AreEqual(new Dictionary<IntPtr, double>
                {
                    {new IntPtr(1), 2D / 3D},
                    {new IntPtr(2), 1D / 3D},
                }, frequency.GetScores(), new ScoreComparer());
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
                var windowEvents = new StubIWindowEvents();
                var frequency = new Frequency(windowEvents);
                frequency.OrderChanged += (sender, args) => called = true;
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), frequency.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                windowEvents.SetupEvent.Invoke(windowEvents, new List<WindowRecord>
                {
                    new WindowRecord(new IntPtr(1))
                });

                System.Fakes.ShimDateTime.NowGet = () => firstIntervalTime;
                frequency.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    { new IntPtr(1), 1 }
                }, frequency.GetScores());
                Assert.IsFalse(called);

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
                var windowEvents = new StubIWindowEvents();
                var frequency = new Frequency(windowEvents);
                frequency.OrderChanged += (sender, args) => called = true;
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), frequency.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                windowEvents.SetupEvent.Invoke(windowEvents, new List<WindowRecord>
                {
                    new WindowRecord(new IntPtr(1))
                });

                System.Fakes.ShimDateTime.NowGet = () => firstIntervalTime;
                frequency.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    { new IntPtr(1), 1 }
                }, frequency.GetScores());
                Assert.IsFalse(called);

                System.Fakes.ShimDateTime.NowGet = () => focusTime;
                windowEvents.WindowOpenedOrFocusedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(2)));

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
                var windowEvents = new StubIWindowEvents();
                var frequency = new Frequency(windowEvents);
                frequency.OrderChanged += (sender, args) => called = true;
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), frequency.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                windowEvents.SetupEvent.Invoke(windowEvents, new List<WindowRecord>
                {
                    new WindowRecord(new IntPtr(1))
                });

                System.Fakes.ShimDateTime.NowGet = () => intervalTime;
                frequency.OnInterval(null, null);

                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>
                {
                    { new IntPtr(1), 1 }
                }, frequency.GetScores());
                Assert.IsFalse(called);

                var nextIntervalTime = intervalTime.AddSeconds(Settings.FrequencyIntervalSeconds);
                while (nextIntervalTime < frequencyTime)
                {
                    var thisIntervalTime = nextIntervalTime;
                    System.Fakes.ShimDateTime.NowGet = () => thisIntervalTime;
                    frequency.OnInterval(null, null);
                    Assert.IsFalse(called, $"{setWindowsTime} - {intervalTime} - {thisIntervalTime} - {frequencyTime}");
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

                var windowEvents = new StubIWindowEvents();
                var frequency = new Frequency(windowEvents);
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), frequency.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                windowEvents.SetupEvent.Invoke(windowEvents, new List<WindowRecord>
                {
                    new WindowRecord(new IntPtr(1))
                });

                System.Fakes.ShimDateTime.NowGet = () => focusEventTime;
                windowEvents.WindowOpenedOrFocusedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(2)));

                System.Fakes.ShimDateTime.NowGet = () => closedEventTime;
                windowEvents.WindowClosedOrMinimizedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(2)));

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

                var windowEvents = new StubIWindowEvents();
                var frequency = new Frequency(windowEvents);
                var called = false;
                frequency.OrderChanged += (sender, args) => called = true;
                CollectionAssert.AreEquivalent(new Dictionary<IntPtr, double>(), frequency.GetScores());

                System.Fakes.ShimDateTime.NowGet = () => setWindowsTime;
                windowEvents.SetupEvent.Invoke(windowEvents, new List<WindowRecord>
                {
                    new WindowRecord(new IntPtr(1))
                });

                System.Fakes.ShimDateTime.NowGet = () => focusEventTime;
                windowEvents.WindowOpenedOrFocusedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(2)));

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
                windowEvents.WindowClosedOrMinimizedEvent.Invoke(windowEvents, new WindowRecord(new IntPtr(2)));

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
