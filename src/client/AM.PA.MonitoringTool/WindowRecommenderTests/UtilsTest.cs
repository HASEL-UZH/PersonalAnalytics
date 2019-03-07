using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using WindowRecommender;

namespace WindowRecommenderTests
{
    [TestClass]
    public class UtilsTest
    {
        [TestMethod]
        public void TestPairwise()
        {
            var source = new[] { "a", "b", "c", "d" };
            var result = source.Pairwise((one, two) => one + two);
            CollectionAssert.AreEqual(new[] { "ab", "bc", "cd" }, result.ToList());
        }

        [TestMethod]
        public void TestPairwise_Empty()
        {
            var source = new string[0];
            var result = source.Pairwise((one, two) => one + two);
            CollectionAssert.AreEqual(new string[0], result.ToList());
        }

        [TestMethod]
        public void TestPairwise_Single()
        {
            var source = new[] { "a" };
            var result = source.Pairwise((one, two) => one + two);
            CollectionAssert.AreEqual(new string[0], result.ToList());
        }
    }
}
