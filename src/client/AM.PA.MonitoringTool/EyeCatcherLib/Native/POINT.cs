using System.Drawing;
using System.Runtime.InteropServices;

namespace EyeCatcherLib.Native
{
    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
    internal struct POINT
    {
        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }

        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public int X;
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public int Y;


        public static implicit operator POINT(Point point)
        {
            return new POINT(point.X, point.Y);
        }

        public static explicit operator Point(POINT point)
        {
            return new Point(point.X, point.Y);
        }
    }

}
