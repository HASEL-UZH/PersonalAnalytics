using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowRecommender.Models;
using WindowRecommender.Models.Fakes;

namespace WindowRecommenderTests.Models
{
    [TestClass]
    public class ModelCoreTest
    {
        [TestMethod]
        public void TestStart()
        {
            var stubModel = new StubIModel
            {
                GetScores = () => new Dictionary<IntPtr, double> {
                    { new IntPtr(1), 1 },
                }
            };

            var modelCore = new ModelCore(new (IModel, double)[]
            {
                (stubModel, 1)
            });
            modelCore.ScoreChanged += (sender, scores) =>
            {
                CollectionAssert.AreEqual(scores, new Dictionary<IntPtr, double>
                {
                    {new IntPtr(1), 1},
                });
            };
            modelCore.Start();
        }

        [TestMethod]
        public void TestEmptyEvent()
        {
            var stubModel = new StubIModel
            {
                GetScores = () => new Dictionary<IntPtr, double>
                {
                    {new IntPtr(1), 0.1},
                }
            };
            var modelCore = new ModelCore(new (IModel, double)[]
            {
                (stubModel, 1),
            });
            modelCore.Start();
        }

        [TestMethod]
        public void TestScoreCalculation()
        {
            var stubModel = new StubIModel
            {
                GetScores = () => new Dictionary<IntPtr, double>
                    {
                        {new IntPtr(1), 1},
                        {new IntPtr(2), 1},
                        {new IntPtr(3), 0},
                        {new IntPtr(4), 0}
                    }
            };

            var stubModel2 = new StubIModel
            {
                GetScores = () => new Dictionary<IntPtr, double>
                    {
                        {new IntPtr(1), 4},
                        {new IntPtr(2), 0},
                        {new IntPtr(3), 1}
                    }
            };

            var modelCore = new ModelCore(new (IModel, double)[]
            {
                    (stubModel, 1),
                    (stubModel2, 2),
            });
            modelCore.ScoreChanged += (sender, scores) =>
            {
                CollectionAssert.AreEqual(scores, new Dictionary<IntPtr, double>
                {
                        {new IntPtr(1), 0.7},
                        {new IntPtr(2), 0.5 / 3},
                        {new IntPtr(3), 0.4 / 3},
                        {new IntPtr(4), 0}
                });
            };
            modelCore.Start();
        }

        [TestMethod]
        public void TestOnScoreChanged()
        {
            var stubModel = new StubIModel
            {
                GetScores = () => new Dictionary<IntPtr, double>
                    {
                        {new IntPtr(1), 1},
                        {new IntPtr(2), 1},
                        {new IntPtr(3), 0},
                        {new IntPtr(4), 0}
                    }
            };

            var stubModel2 = new StubIModel
            {
                GetScores = () => new Dictionary<IntPtr, double>
                    {
                        {new IntPtr(1), 4},
                        {new IntPtr(2), 0},
                        {new IntPtr(3), 1}
                    }
            };

            var modelCore = new ModelCore(new (IModel, double)[]
            {
                    (stubModel, 0.5),
                    (stubModel2, 1),
            });
            modelCore.ScoreChanged += (sender, scores) =>
            {
                CollectionAssert.AreEqual(scores, new Dictionary<IntPtr, double>
                {
                        {new IntPtr(1), 0.7},
                        {new IntPtr(2), 0.5 / 3},
                        {new IntPtr(3), 0.4 / 3},
                        {new IntPtr(4), 0}
                });
            };
            stubModel.ScoreChangedEvent.Invoke(stubModel, null);
        }

        [TestMethod]
        public void TestOnScoreChanged_NoChange()
        {
            var windowChanged = false;
            void OnScoreChanged(object sender, Dictionary<IntPtr, double> scores)
            {
                if (!windowChanged)
                {
                    CollectionAssert.AreEqual(new Dictionary<IntPtr, double>
                {
                        {new IntPtr(1), 0.6},
                        {new IntPtr(2), 0.4},
                }, scores);
                    windowChanged = true;
                }
                else
                {
                    Assert.Fail();
                }
            }

            var firstCall = true;
            Dictionary<IntPtr, double> GetScores()
            {
                if (firstCall)
                {
                    firstCall = false;
                    return new Dictionary<IntPtr, double>
                    {
                        {new IntPtr(1), 0.6},
                        {new IntPtr(2), 0.4},
                    };
                }
                return new Dictionary<IntPtr, double>
                {
                    {new IntPtr(2), 0.4},
                    {new IntPtr(1), 0.6},
                };
            }

            var stubModel = new StubIModel
            {
                GetScores = GetScores,
            };
            var modelCore = new ModelCore(new (IModel, double)[]
            {
                (stubModel, 1),
            });

            modelCore.ScoreChanged += OnScoreChanged;
            modelCore.Start();

            stubModel.ScoreChangedEvent.Invoke(stubModel, null);
            CollectionAssert.AreEqual(new List<IntPtr>
            {
                new IntPtr(1),
                new IntPtr(2),
            }, modelCore.GetTopWindows());
        }

        [TestMethod]
        public void TestOnWindowsChanged()
        {
            var windowChanged = false;
            void OnWindowsChange(object sender, List<IntPtr> topWindows)
            {
                if (!windowChanged)
                {
                    CollectionAssert.AreEqual(new List<IntPtr>
                    {
                        new IntPtr(1),
                        new IntPtr(2),
                    }, topWindows);
                    windowChanged = true;
                }
                else
                {
                    Assert.Fail();
                }
            }

            var firstCall = true;
            Dictionary<IntPtr, double> GetScores()
            {
                if (firstCall)
                {
                    firstCall = false;
                    return new Dictionary<IntPtr, double>
                    {
                        {new IntPtr(1), 0.6},
                        {new IntPtr(2), 0.4},
                    };
                }
                return new Dictionary<IntPtr, double>
                {
                    {new IntPtr(1), 0.6},
                    {new IntPtr(2), 0.5},
                };
            }

            var stubModel = new StubIModel
            {
                GetScores = GetScores,
            };
            var modelCore = new ModelCore(new (IModel, double)[]
            {
                (stubModel, 1),
            });

            modelCore.WindowsChanged += OnWindowsChange;
            modelCore.Start();

            stubModel.ScoreChangedEvent.Invoke(stubModel, null);
            CollectionAssert.AreEqual(new List<IntPtr>
            {
                new IntPtr(1),
                new IntPtr(2),
            }, modelCore.GetTopWindows());
        }

        [TestMethod]
        public void TestNormalizeScores_Empty()
        {
            var scores = new Dictionary<IntPtr, double>();
            CollectionAssert.AreEqual(scores, ModelCore.NormalizeScores(scores));
        }

        [TestMethod]
        public void TestNormalizeScores_ZeroSum()
        {
            var scores = new Dictionary<IntPtr, double>
            {
                { new IntPtr(1), 0 }
            };
            CollectionAssert.AreEqual(scores, ModelCore.NormalizeScores(scores));
        }

        [TestMethod]
        public void TestNormalizeScores()
        {
            CollectionAssert.AreEqual(new Dictionary<IntPtr, double>
            {
                { new IntPtr(1), 0.2 },
                { new IntPtr(2), 0.4 },
                { new IntPtr(3), 0.4 }
            }, ModelCore.NormalizeScores(new Dictionary<IntPtr, double>
            {
                { new IntPtr(1), 0.1 },
                { new IntPtr(2), 0.2 },
                { new IntPtr(3), 0.2 }
            }));
        }
    }
}
