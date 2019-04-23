using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowRecommender;
using WindowRecommender.Fakes;
using WindowRecommender.Models;
using WindowRecommender.Native.Fakes;
using WindowRecommenderTests.Tools;

namespace WindowRecommenderTests
{
    [TestClass]
    public class TitleSimilarityTest
    {
        [TestMethod]
        public void TestEmpty()
        {
            var titleSimilarity = new TitleSimilarity(new ModelEvents());
            titleSimilarity.SetWindows(new List<IntPtr>());
            CollectionAssert.AreEqual(new Dictionary<IntPtr, double>(), titleSimilarity.GetScores());
        }

        [TestMethod]
        public void TestScore_EmptyTitles()
        {
            var titles = new Dictionary<IntPtr, string>
            {
                {new IntPtr(1), ""},
                {new IntPtr(2), ""},
            };
            using (ShimsContext.Create())
            {
                ShimNativeMethods.GetWindowTitleIntPtr = windowHandle => titles[windowHandle];
                var titleSimilarity = new TitleSimilarity(new ModelEvents());
                titleSimilarity.SetWindows(titles.Keys.ToList());
                CollectionAssert.AreEqual(new Dictionary<IntPtr, double>(), titleSimilarity.GetScores());
            }
        }

        [TestMethod]
        public void TestScore_EqualTitles()
        {
            var titles = new Dictionary<IntPtr, string>
            {
                {new IntPtr(1), "Very nice title"},
                {new IntPtr(2), "Very nice title"},
            };
            using (ShimsContext.Create())
            {
                ShimNativeMethods.GetWindowTitleIntPtr = windowHandle => titles[windowHandle];
                var titleSimilarity = new TitleSimilarity(new ModelEvents());
                titleSimilarity.SetWindows(titles.Keys.ToList());
                CollectionAssert.AreEqual(new Dictionary<IntPtr, double>
                {
                    {new IntPtr(2), 1},
                }, titleSimilarity.GetScores(), new ScoreComparer());
            }
        }

        [TestMethod]
        public void TestScore_NoOverlap()
        {
            var titles = new Dictionary<IntPtr, string>
            {
                {new IntPtr(1), "Very nice title"},
                {new IntPtr(2), "Not good text"},
            };
            using (ShimsContext.Create())
            {
                ShimNativeMethods.GetWindowTitleIntPtr = windowHandle => titles[windowHandle];
                var titleSimilarity = new TitleSimilarity(new ModelEvents());
                titleSimilarity.SetWindows(titles.Keys.ToList());
                CollectionAssert.AreEqual(new Dictionary<IntPtr, double>(), titleSimilarity.GetScores());
            }
        }

        [TestMethod]
        public void TestScore_Subset()
        {
            var titles = new Dictionary<IntPtr, string>
            {
                {new IntPtr(1), "Very nice title"},
                {new IntPtr(2), "Not good title"},
                {new IntPtr(3), "Nice title"},
            };
            using (ShimsContext.Create())
            {
                ShimNativeMethods.GetWindowTitleIntPtr = windowHandle => titles[windowHandle];
                var titleSimilarity = new TitleSimilarity(new ModelEvents());
                titleSimilarity.SetWindows(titles.Keys.ToList());
                var scores = titleSimilarity.GetScores();
                Assert.IsTrue(scores[new IntPtr(3)] > scores[new IntPtr(2)]);
            }
        }

        [TestMethod]
        public void TestScore_DocumentLength()
        {
            var titles = new Dictionary<IntPtr, string>
            {
                {new IntPtr(1), "Very nice title"},
                {new IntPtr(2), "Not good title"},
                {new IntPtr(3), "Nice text"},
            };
            using (ShimsContext.Create())
            {
                ShimNativeMethods.GetWindowTitleIntPtr = windowHandle => titles[windowHandle];
                var titleSimilarity = new TitleSimilarity(new ModelEvents());
                titleSimilarity.SetWindows(titles.Keys.ToList());
                var scores = titleSimilarity.GetScores();
                Assert.IsTrue(scores[new IntPtr(3)] > scores[new IntPtr(2)]);
            }
        }

        [TestMethod]
        public void TestOnWindowClosed()
        {
            var titles = new Dictionary<IntPtr, string>
            {
                {new IntPtr(1), "Very nice title"},
                {new IntPtr(2), "Very nice title"},
            };
            using (ShimsContext.Create())
            {
                var called = false;
                EventHandler<IntPtr> closedHandler = null;
                ShimNativeMethods.GetWindowTitleIntPtr = windowHandle => titles[windowHandle];
                var modelEvents = new ShimModelEvents
                {
                    WindowClosedAddEventHandlerOfIntPtr = handler => closedHandler = handler
                };
                var titleSimilarity = new TitleSimilarity(modelEvents);
                titleSimilarity.OrderChanged += (sender, args) => called = true;
                titleSimilarity.SetWindows(titles.Keys.ToList());
                CollectionAssert.AreEqual(new Dictionary<IntPtr, double>
                {
                    {new IntPtr(2), 1},
                }, titleSimilarity.GetScores(), new ScoreComparer());

                closedHandler.Invoke(modelEvents, new IntPtr(2));
                CollectionAssert.AreEqual(new Dictionary<IntPtr, double>(), titleSimilarity.GetScores());
                Assert.IsFalse(called);
            }
        }

        [TestMethod]
        public void TestOnWindowFocused()
        {
            var titles = new Dictionary<IntPtr, string>
            {
                {new IntPtr(1), "one two three"},
                {new IntPtr(2), "two three four"},
                {new IntPtr(3), "three four five"},
                {new IntPtr(4), "four five six"},
            };
            using (ShimsContext.Create())
            {
                var called = false;
                EventHandler<IntPtr> focusedHandler = null;
                ShimNativeMethods.GetWindowTitleIntPtr = windowHandle => titles[windowHandle];
                var modelEvents = new ShimModelEvents
                {
                    WindowFocusedAddEventHandlerOfIntPtr = handler => focusedHandler = handler
                };
                var titleSimilarity = new TitleSimilarity(modelEvents);
                titleSimilarity.OrderChanged += (sender, args) => called = true;
                titleSimilarity.SetWindows(titles.Keys.ToList());
                var scores = titleSimilarity.GetScores();
                Assert.AreEqual(2, scores.Count);
                Assert.IsTrue(scores[new IntPtr(2)] > scores[new IntPtr(3)]);
                Assert.IsFalse(called);

                focusedHandler.Invoke(modelEvents, new IntPtr(4));
                scores = titleSimilarity.GetScores();
                Assert.AreEqual(2, scores.Count);
                Assert.IsTrue(scores[new IntPtr(3)] > scores[new IntPtr(2)]);
                Assert.IsTrue(called);
            }
        }

        [TestMethod]
        public void TestOnWindowOpened()
        {
            var titles = new Dictionary<IntPtr, string>
            {
                {new IntPtr(1), "one two three"},
                {new IntPtr(2), "two three four"},
                {new IntPtr(3), "three four five"},
                {new IntPtr(4), "four five six"},
            };
            using (ShimsContext.Create())
            {
                var called = false;
                EventHandler<IntPtr> openedHandler = null;
                ShimNativeMethods.GetWindowTitleIntPtr = windowHandle => titles[windowHandle];
                var modelEvents = new ShimModelEvents
                {
                    WindowOpenedAddEventHandlerOfIntPtr = handler => openedHandler = handler
                };
                var titleSimilarity = new TitleSimilarity(modelEvents);
                titleSimilarity.OrderChanged += (sender, args) => called = true;
                titleSimilarity.SetWindows(titles.Keys.Take(3).ToList());
                var scores = titleSimilarity.GetScores();
                Assert.AreEqual(2, scores.Count);
                Assert.IsTrue(scores[new IntPtr(2)] > scores[new IntPtr(3)]);
                Assert.IsFalse(called);

                openedHandler.Invoke(modelEvents, new IntPtr(4));
                scores = titleSimilarity.GetScores();
                Assert.AreEqual(2, scores.Count);
                Assert.IsTrue(scores[new IntPtr(3)] > scores[new IntPtr(2)]);
                Assert.IsTrue(called);
            }
        }

        [TestMethod]
        public void TestOnWindowOpened_EmptyTitle()
        {
            var titles = new Dictionary<IntPtr, string>
            {
                {new IntPtr(1), "one two three"},
                {new IntPtr(2), "two three four"},
                {new IntPtr(3), "three four five"},
                {new IntPtr(4), ""},
            };
            using (ShimsContext.Create())
            {
                var called = false;
                EventHandler<IntPtr> openedHandler = null;
                ShimNativeMethods.GetWindowTitleIntPtr = windowHandle => titles[windowHandle];
                var modelEvents = new ShimModelEvents
                {
                    WindowOpenedAddEventHandlerOfIntPtr = handler => openedHandler = handler
                };
                var titleSimilarity = new TitleSimilarity(modelEvents);
                titleSimilarity.OrderChanged += (sender, args) => called = true;
                titleSimilarity.SetWindows(titles.Keys.Take(3).ToList());
                var scores = titleSimilarity.GetScores();
                Assert.AreEqual(2, scores.Count);
                Assert.IsTrue(scores[new IntPtr(2)] > scores[new IntPtr(3)]);
                Assert.IsFalse(called);

                openedHandler.Invoke(modelEvents, new IntPtr(4));
                CollectionAssert.AreEqual(new Dictionary<IntPtr, double>(), titleSimilarity.GetScores());
                Assert.IsTrue(called);
            }
        }

        [TestMethod]
        public void TestOnWindowRenamed_SameTitle()
        {
            var titles = new Dictionary<IntPtr, string>
            {
                {new IntPtr(1), "one two three"},
                {new IntPtr(2), "two three four"},
                {new IntPtr(3), "three four five"},
            };
            using (ShimsContext.Create())
            {
                var called = false;
                EventHandler<IntPtr> renamedHandler = null;
                ShimNativeMethods.GetWindowTitleIntPtr = windowHandle => titles[windowHandle];
                var modelEvents = new ShimModelEvents
                {
                    WindowRenamedAddEventHandlerOfIntPtr = handler => renamedHandler = handler
                };
                var titleSimilarity = new TitleSimilarity(modelEvents);
                titleSimilarity.OrderChanged += (sender, args) => called = true;
                titleSimilarity.SetWindows(titles.Keys.ToList());
                var initialScores = titleSimilarity.GetScores();
                Assert.AreEqual(2, initialScores.Count);
                Assert.IsTrue(initialScores[new IntPtr(2)] > initialScores[new IntPtr(3)]);
                Assert.IsFalse(called);

                renamedHandler.Invoke(modelEvents, new IntPtr(2));
                var renamedScores = titleSimilarity.GetScores();
                Assert.AreEqual(2, renamedScores.Count);
                Assert.IsTrue(renamedScores[new IntPtr(2)] > renamedScores[new IntPtr(3)]);
                Assert.IsFalse(called);
            }
        }

        [TestMethod]
        public void TestOnWindowRenamed_Changed()
        {
            var titles = new Dictionary<IntPtr, string>
            {
                {new IntPtr(1), "one two three four"},
                {new IntPtr(2), "two three four five"},
                {new IntPtr(3), "three four five six"},
            };
            using (ShimsContext.Create())
            {
                var called = false;
                EventHandler<IntPtr> renamedHandler = null;
                ShimNativeMethods.GetWindowTitleIntPtr = windowHandle => titles[windowHandle];
                var modelEvents = new ShimModelEvents
                {
                    WindowRenamedAddEventHandlerOfIntPtr = handler => renamedHandler = handler
                };
                var titleSimilarity = new TitleSimilarity(modelEvents);
                titleSimilarity.OrderChanged += (sender, args) => called = true;
                titleSimilarity.SetWindows(titles.Keys.ToList());
                var initialScores = titleSimilarity.GetScores();
                Assert.AreEqual(2, initialScores.Count);
                Assert.IsTrue(initialScores[new IntPtr(2)] > initialScores[new IntPtr(3)]);
                Assert.IsFalse(called);

                titles[new IntPtr(2)] = "four five six seven";
                renamedHandler.Invoke(modelEvents, new IntPtr(2));
                var renamedScores = titleSimilarity.GetScores();
                Assert.AreEqual(2, renamedScores.Count);
                Assert.IsTrue(renamedScores[new IntPtr(3)] > renamedScores[new IntPtr(2)]);
                Assert.IsTrue(called);
            }
        }

        [TestMethod]
        public void TestOnWindowRenamed_Empty()
        {
            var titles = new Dictionary<IntPtr, string>
            {
                {new IntPtr(1), "one two three four"},
                {new IntPtr(2), "two three four five"},
                {new IntPtr(3), "three four five six"},
            };
            using (ShimsContext.Create())
            {
                var called = false;
                EventHandler<IntPtr> renamedHandler = null;
                ShimNativeMethods.GetWindowTitleIntPtr = windowHandle => titles[windowHandle];
                var modelEvents = new ShimModelEvents
                {
                    WindowRenamedAddEventHandlerOfIntPtr = handler => renamedHandler = handler
                };
                var titleSimilarity = new TitleSimilarity(modelEvents);
                titleSimilarity.OrderChanged += (sender, args) => called = true;
                titleSimilarity.SetWindows(titles.Keys.ToList());
                var initialScores = titleSimilarity.GetScores();
                Assert.AreEqual(2, initialScores.Count);
                Assert.IsTrue(initialScores[new IntPtr(2)] > initialScores[new IntPtr(3)]);
                Assert.IsFalse(called);

                titles[new IntPtr(2)] = "";
                renamedHandler.Invoke(modelEvents, new IntPtr(2));
                var renamedScores = titleSimilarity.GetScores();
                Assert.AreEqual(1, renamedScores.Count);
                Assert.IsTrue(renamedScores.ContainsKey(new IntPtr(3)));
                Assert.IsTrue(called);
            }
        }

        [TestMethod]
        public void TestOnWindowRenamed_New()
        {
            var titles = new Dictionary<IntPtr, string>
            {
                {new IntPtr(1), "one two three four"},
                {new IntPtr(3), "three four five six"},
            };
            using (ShimsContext.Create())
            {
                var called = false;
                EventHandler<IntPtr> renamedHandler = null;
                ShimNativeMethods.GetWindowTitleIntPtr = windowHandle => titles[windowHandle];
                var modelEvents = new ShimModelEvents
                {
                    WindowRenamedAddEventHandlerOfIntPtr = handler => renamedHandler = handler
                };
                var titleSimilarity = new TitleSimilarity(modelEvents);
                titleSimilarity.OrderChanged += (sender, args) => called = true;
                titleSimilarity.SetWindows(titles.Keys.ToList());
                var initialScores = titleSimilarity.GetScores();
                Assert.AreEqual(1, initialScores.Count);
                Assert.IsTrue(initialScores.ContainsKey(new IntPtr(3)));
                Assert.IsFalse(called);

                titles[new IntPtr(2)] = "two three four five";
                renamedHandler.Invoke(modelEvents, new IntPtr(2));
                var renamedScores = titleSimilarity.GetScores();
                Assert.AreEqual(2, renamedScores.Count);
                Assert.IsTrue(renamedScores[new IntPtr(2)] > initialScores[new IntPtr(3)]);
                Assert.IsTrue(called);
            }
        }
    }
}
