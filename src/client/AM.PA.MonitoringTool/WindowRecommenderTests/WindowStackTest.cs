using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using WindowRecommender;
using WindowRecommender.Fakes;

namespace WindowRecommenderTests
{
    [TestClass]
    public class WindowStackTest
    {
        [TestMethod]
        public void TestGet_Set()
        {
            var windows = new List<IntPtr> { new IntPtr(1) };
            var windowStack = new WindowStack(new ModelEvents()) { Windows = windows };
            CollectionAssert.AreEqual(windows, windowStack.Windows);
        }

        [TestMethod]
        public void TestGet_Focus()
        {
            using (ShimsContext.Create())
            {
                EventHandler<IntPtr> focusHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowFocusedAddEventHandlerOfIntPtr = handler => focusHandler = handler
                };
                var windowStack = new WindowStack(modelEvents)
                {
                    Windows = new List<IntPtr>
                    {
                        new IntPtr(1),
                        new IntPtr(2)
                    }
                };
                focusHandler.Invoke(modelEvents, new IntPtr(2));
                CollectionAssert.AreEqual(new List<IntPtr>
                {
                    new IntPtr(2),
                    new IntPtr(1)
                }, windowStack.Windows);
            }
        }

        [TestMethod]
        public void TestGet_Close()
        {
            using (ShimsContext.Create())
            {
                EventHandler<IntPtr> closeHandler = null;
                var modelEvents = new ShimModelEvents
                {
                    WindowClosedAddEventHandlerOfIntPtr = handler => closeHandler = handler
                };
                var windowStack = new WindowStack(modelEvents)
                {
                    Windows = new List<IntPtr>
                    {
                        new IntPtr(1),
                        new IntPtr(2)
                    }
                };
                closeHandler.Invoke(modelEvents, new IntPtr(1));
                CollectionAssert.AreEqual(new List<IntPtr>
                {
                    new IntPtr(2)
                }, windowStack.Windows);
            }
        }

        [TestMethod]
        public void TestGetZIndex()
        {
            var windowStack = new WindowStack(new ModelEvents())
            {
                Windows = new List<IntPtr>
                {
                    new IntPtr(1),
                    new IntPtr(2)
                }
            };
            Assert.AreEqual(1, windowStack.GetZIndex(new IntPtr(2)));
        }
    }
}
