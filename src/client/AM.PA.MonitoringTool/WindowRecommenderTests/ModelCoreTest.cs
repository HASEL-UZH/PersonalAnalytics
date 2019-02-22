using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using WindowRecommender;
using WindowRecommender.Fakes;
using WindowRecommender.Native;
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
                ShimNativeMethods.GetWindowRectangleIntPtr = ptr => new RECT(1, 2, 3, 4);

                var stubModel = new StubIModel
                {
                    SetWindowsListOfIntPtr = ptrs => Assert.AreEqual(new IntPtr(1), ptrs[0]),
                    GetScores = () => new Dictionary<IntPtr, double> { { new IntPtr(1), 1 } }
                };

                var modelCore = new ModelCore(new Dictionary<IModel, int>
                {
                    {stubModel, 1}
                });
                modelCore.WindowsHaze += (sender, rectangles) =>
                {
                    var rectangleList = rectangles.ToList();
                    Assert.AreEqual(1, rectangleList.Count);
                    Assert.AreEqual(1, rectangleList[0].Left);
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
                var i = 1;
                ShimNativeMethods.GetWindowRectangleIntPtr = ptr =>
                {
                    Assert.AreEqual(new IntPtr(i), ptr);
                    i++;
                    return new RECT(1, 2, 3, 4);
                };

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
                modelCore.WindowsHaze += (sender, rectangles) =>
                {
                    var rectangleList = rectangles.ToList();
                    Assert.AreEqual(Settings.NumberOfWindows, rectangleList.Count);
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
                var i = 1;
                ShimNativeMethods.GetWindowRectangleIntPtr = ptr =>
                {
                    Assert.AreEqual(new IntPtr(i), ptr);
                    i++;
                    return new RECT(1, 2, 3, 4);
                };

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
                modelCore.WindowsHaze += (sender, rectangles) =>
                {
                    var rectangleList = rectangles.ToList();
                    Assert.AreEqual(Settings.NumberOfWindows, rectangleList.Count);
                };
                stubModel.OrderChangedEvent.Invoke(stubModel, null);
            }
        }
    }
}
