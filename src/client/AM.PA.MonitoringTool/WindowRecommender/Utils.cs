using System;
using System.Collections.Generic;

namespace WindowRecommender
{
    internal static class Utils
    {
        /// <summary>
        /// Iterate over a an enumerable in pairs.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector" />.</typeparam>
        /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements to pair.</param>
        /// <param name="resultSelector">A function to create a result value from each pair.</param>
        /// <returns>A collection of elements of type <typeparamref name="TResult" /> where each element represents a projection over a pair.</returns>
        /// https://stackoverflow.com/a/1581482/1469028
        public static IEnumerable<TResult> Pairwise<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TSource, TResult> resultSelector)
        {
            var previous = default(TSource);
            using (var it = source.GetEnumerator())
            {
                if (it.MoveNext())
                {
                    previous = it.Current;
                }
                while (it.MoveNext())
                {
                    yield return resultSelector(previous, previous = it.Current);
                }
            }
        }
    }
}
