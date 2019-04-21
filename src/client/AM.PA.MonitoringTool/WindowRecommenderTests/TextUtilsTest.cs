using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowRecommender;

namespace WindowRecommenderTests
{
    [TestClass]
    public class TextUtilsTest
    {
        [TestMethod]
        public void TestPrepareTitle_Replace()
        {
            var tests = new[]
            {
                (input: "", expected: new string[0]),
                (input: "x-b", expected: new[] {"x-b"}),
                (input: "x'b", expected: new[] {"x'b"}),
                (input: "xbc", expected: new[] {"xbc"}),
                (input: "öüß", expected: new[] {"öüß"}),
                (input: "123", expected: new string[0]),
                (input: "1 ?", expected: new string[0]),
                (input: "x1b", expected: new[] {"x", "b"}),
                (input: "x_b", expected: new[] {"x", "b"}),
            };
            foreach (var (input, expected) in tests)
            {
                var actual = TextUtils.PrepareTitle(input).ToArray();
                CollectionAssert.AreEqual(expected, actual, $"input: {input}, expected: {expected}, actual: {actual}");
            }
        }

        [TestMethod]
        public void TestPrepareTitle_Split()
        {
            var tests = new[]
            {
                (input: "", expected: new string[0]),
                (input: " ", expected: new string[0]),
                (input: "  ", expected: new string[0]),
                (input: " x ", expected: new[] {"x"}),
                (input: "  x", expected: new[] {"x"}),
                (input: "x b", expected: new[] {"x", "b"}),
            };
            foreach (var (input, expected) in tests)
            {
                var actual = TextUtils.PrepareTitle(input).ToArray();
                CollectionAssert.AreEqual(expected, actual, $"input: {input}, expected: {expected}, actual: {actual}");
            }
        }

        [TestMethod]
        public void TestPrepareTitle_StopWords()
        {
            var tests = new[]
            {
                (input: "a b", expected: new[] {"b"}),
            };
            foreach (var (input, expected) in tests)
            {
                var actual = TextUtils.PrepareTitle(input).ToArray();
                CollectionAssert.AreEqual(expected, actual, $"input: {input}, expected: {expected}, actual: {actual}");
            }
        }

        [TestMethod]
        public void TestPrepareTitle_ToLower()
        {
            var tests = new[]
            {
                (input: "XX", expected: new[] {"xx"}),
            };
            foreach (var (input, expected) in tests)
            {
                var actual = TextUtils.PrepareTitle(input).ToArray();
                CollectionAssert.AreEqual(expected, actual, $"input: {input}, expected: {expected}, actual: {actual}");
            }
        }
    }
}