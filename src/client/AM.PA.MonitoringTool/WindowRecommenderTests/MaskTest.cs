using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using WindowRecommender;

namespace WindowRecommenderTests
{
    [TestClass]
    public class MaskTest
    {
        [TestMethod]
        public void TestCutSingle_NoOverlap()
        {
            var source = new Rectangle(0, 0, 1, 1);
            var cover = new Rectangle(2, 2, 3, 3);
            var rects = Mask.Cut(source, cover);
            CollectionAssert.AreEqual(new List<Rectangle> { source }, rects);
        }

        [TestMethod]
        public void TestCutSingle_Corner()
        {
            var source = new Rectangle(1, 0, 3, 2);
            var cover = new Rectangle(0, 1, 2, 3);
            var rects = Mask.Cut(source, cover);
            CollectionAssert.AreEqual(new List<Rectangle>
            {
                new Rectangle(1, 0, 3, 1),
                new Rectangle(2, 1, 3, 2)
            }, rects);
        }

        [TestMethod]
        public void TestCutSingle_Side()
        {
            var source = new Rectangle(1, 0, 3, 3);
            var cover = new Rectangle(0, 1, 2, 2);
            var rects = Mask.Cut(source, cover);
            CollectionAssert.AreEqual(new List<Rectangle>
            {
                new Rectangle(1, 0, 3, 1),
                new Rectangle(2, 1, 3, 3),
                new Rectangle(1, 2, 2, 3)
            }, rects);
        }

        [TestMethod]
        public void TestCutSingle_Inside()
        {
            var source = new Rectangle(0, 0, 3, 3);
            var cover = new Rectangle(1, 1, 2, 2);
            var rects = Mask.Cut(source, cover);
            CollectionAssert.AreEqual(new List<Rectangle>
            {
                new Rectangle(0, 0, 1, 2),
                new Rectangle(1, 0, 3, 1),
                new Rectangle(2, 1, 3, 3),
                new Rectangle(0, 2, 2, 3)
            }, rects);
        }
    }
}
