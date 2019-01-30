using GameOverlay.Graphics.Primitives;
using System;
using System.Collections.Generic;

namespace WindowRecommender
{
    internal static class Mask
    {
        internal static List<Rectangle> Cut(Rectangle screen, List<Rectangle> windows)
        {
            var output = new List<Rectangle> { screen };
            foreach (var window in windows)
            {
                var rects = output;
                output = new List<Rectangle>();
                foreach (var rectangle in rects)
                {
                    output.AddRange(Cut(rectangle, window));
                }
            }
            return output;
        }

        /// <summary>
        /// Cut a rectangle out of another source rectangle.
        /// </summary>
        /// <param name="source">Rectangle to but cut up.</param>
        /// <param name="cover">Covering Rectangle.</param>
        /// <returns>List of rectangle required for source minus cover.</returns>
        internal static List<Rectangle> Cut(Rectangle source, Rectangle cover)
        {
            var rects = new List<Rectangle>();
            if (IntersectsWith(source, cover))
            {
                if (source.Left < cover.Left)
                {
                    rects.Add(new Rectangle(source.Left, source.Top, cover.Left, Math.Min(source.Bottom, cover.Bottom)));
                }
                if (source.Top < cover.Top)
                {
                    rects.Add(new Rectangle(Math.Max(source.Left, cover.Left), source.Top, source.Right, cover.Top));
                }
                if (source.Right > cover.Right)
                {
                    rects.Add(new Rectangle(cover.Right, Math.Max(source.Top, cover.Top), source.Right, source.Bottom));
                }
                if (source.Bottom > cover.Bottom)
                {
                    rects.Add(new Rectangle(source.Left, cover.Bottom, Math.Min(source.Right, cover.Right), source.Bottom));
                }
            }
            else
            {
                rects.Add(source);
            }
            return rects;
        }

        private static bool IntersectsWith(Rectangle a, Rectangle b)
        {
            if (a.Left < b.Right && b.Left < a.Right && a.Top < b.Bottom)
            {
                return b.Top < a.Bottom;
            }
            return false;
        }
    }
}
