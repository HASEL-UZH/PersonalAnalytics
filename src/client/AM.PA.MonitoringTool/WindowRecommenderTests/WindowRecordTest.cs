using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowRecommender;

namespace WindowRecommenderTests
{
    [TestClass]
    public class WindowRecordTest
    {
        [TestMethod]
        public void TestEqual()
        {
            Assert.AreEqual(new WindowRecord(new IntPtr(1)), new WindowRecord(new IntPtr(1)));
            Assert.AreEqual(new WindowRecord(new IntPtr(1), "a", "a"), new WindowRecord(new IntPtr(1), "b", "b"));

            Assert.AreNotEqual(new WindowRecord(new IntPtr(1)), new WindowRecord(new IntPtr(2)));
        }

        [TestMethod]
        public void TestHashcode()
        {
            var dictionary = new Dictionary<WindowRecord, bool>
            {
                {new WindowRecord(new IntPtr(1)), true}
            };
            Assert.IsTrue(dictionary.ContainsKey(new WindowRecord(new IntPtr(1))));
            Assert.IsTrue(dictionary.ContainsKey(new WindowRecord(new IntPtr(1), "a", "b")));
        }
    }
}
