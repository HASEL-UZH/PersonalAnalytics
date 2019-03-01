using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using WindowRecommender;
using WindowRecommender.Fakes;
using WindowRecommender.Native.Fakes;

namespace WindowRecommenderTests
{
    [TestClass]
    public class ModelCoreTest
    {
        [TestMethod]
        public void TestStart()
        {
            using (ShimsContext.Create())
            {
                ShimNativeMethods.GetOpenWindows = () => new List<IntPtr> { new IntPtr(1) };

                var stubModel = new StubIModel
                {
                    SetWindowsListOfIntPtr = windowHandles => Assert.AreEqual(new IntPtr(1), windowHandles[0]),
                    GetScores = () => new Dictionary<IntPtr, double> { { new IntPtr(1), 1 } }
                };

                var modelCore = new ModelCore(new Dictionary<IModel, int>
                {
                    {stubModel, 1}
                });
                modelCore.ScoreChanged += (sender, scores) =>
                {
                    Assert.AreEqual(1, scores.Count);
                    Assert.AreEqual(1, scores[new IntPtr(1)]);
                };
                modelCore.Start();
            }
        }

        [TestMethod]
        public void TestScoreCalculation()
        {
            using (ShimsContext.Create())
            {
                ShimNativeMethods.GetOpenWindows = () => new List<IntPtr>();

                var stubModel = new StubIModel
                {
                    GetScores = () => new Dictionary<IntPtr, double>
                    {
                        { new IntPtr(1), 1 },
                        { new IntPtr(2), 1 },
                        { new IntPtr(3), 0 },
                        { new IntPtr(4), 0 },
                    }
                };

                var stubModel2 = new StubIModel
                {
                    GetScores = () => new Dictionary<IntPtr, double>
                    {
                        { new IntPtr(1), 0.5 },
                        { new IntPtr(2), 0 },
                        { new IntPtr(3), 0.4 },
                        { new IntPtr(4), 0 },
                    }
                };

                var modelCore = new ModelCore(new Dictionary<IModel, int>
                {
                    {stubModel, 1},
                    {stubModel2, 2}
                });
                modelCore.ScoreChanged += (sender, scores) =>
                {
                    Assert.AreEqual(4, scores.Count);
                    Assert.AreEqual(1, scores[new IntPtr(1)]);
                    Assert.AreEqual(0.5, scores[new IntPtr(2)]);
                    Assert.AreEqual(0.4, scores[new IntPtr(3)]);
                    Assert.AreEqual(0, scores[new IntPtr(4)]);
                };
                modelCore.Start();
            }
        }

        [TestMethod]
        public void TestOnOrderChanged()
        {
            using (ShimsContext.Create())
            {
                ShimNativeMethods.GetOpenWindows = () => new List<IntPtr>();

                var stubModel = new StubIModel
                {
                    GetScores = () => new Dictionary<IntPtr, double>
                    {
                        { new IntPtr(1), 1 },
                        { new IntPtr(2), 1 },
                        { new IntPtr(3), 0 },
                        { new IntPtr(4), 0 },
                    }
                };

                var stubModel2 = new StubIModel
                {
                    GetScores = () => new Dictionary<IntPtr, double>
                    {
                        { new IntPtr(1), 0.5 },
                        { new IntPtr(2), 0 },
                        { new IntPtr(3), 0.4 },
                        { new IntPtr(4), 0 },
                    }
                };

                var modelCore = new ModelCore(new Dictionary<IModel, int>
                {
                    {stubModel, 1},
                    {stubModel2, 2}
                });
                modelCore.ScoreChanged += (sender, scores) =>
                {
                    Assert.AreEqual(4, scores.Count);
                    Assert.AreEqual(1, scores[new IntPtr(1)]);
                    Assert.AreEqual(0.5, scores[new IntPtr(2)]);
                    Assert.AreEqual(0.4, scores[new IntPtr(3)]);
                    Assert.AreEqual(0, scores[new IntPtr(4)]);
                };
                stubModel.OrderChangedEvent.Invoke(stubModel, null);
            }
        }
    }
}
