using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowRecommender;

namespace WindowRecommenderTests
{
    [TestClass]
    public class RectangleTest
    {
        [TestMethod]
        public void TestIntersectsWith_BottomRight()
        {
            var a = new Rectangle(0, 0, 10, 10);
            var b = new Rectangle(5, 5, 15, 15);
            Assert.IsTrue(a.IntersectsWith(b));
        }

        [TestMethod]
        public void TestIntersectsWith_None()
        {
            var a = new Rectangle(0, 0, 10, 10);
            var b = new Rectangle(15, 15, 20, 20);
            Assert.IsFalse(a.IntersectsWith(b));
        }

        [TestMethod]
        public void TestIntersectsWith_Same()
        {
            var a = new Rectangle(0, 0, 10, 10);
            var b = new Rectangle(0, 0, 10, 10);
            Assert.IsTrue(a.IntersectsWith(b));
        }

        [TestMethod]
        public void TestIntersectsWith_Left()
        {
            var a = new Rectangle(5, 5, 10, 10);
            var b = new Rectangle(0, 6, 6, 8);
            Assert.IsTrue(a.IntersectsWith(b));
        }
    }
}
