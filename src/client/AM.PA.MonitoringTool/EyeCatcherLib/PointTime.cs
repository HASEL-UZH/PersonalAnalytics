using System;
using System.Drawing;

namespace EyeCatcherLib
{
    public class PointTime
    {
        public PointTime(Point point)
        {
            Point = point;
        }

        public PointTime(int x, int y)
        {
            Point = new Point(x, y);
        }

        public DateTime Time { get; } = DateTime.Now;
        public Point Point { get; set; }
    }
}
