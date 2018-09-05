using System.Drawing;

namespace EyeCatcherLib.Providers
{
    public class FixationPointTime : PointTime
    {
        public FixationPointTime(Point point, string fixationInfo) : base(point)
        {
            FixationInfo = fixationInfo;
        }

        public FixationPointTime(int x, int y, string fixationInfo) : base(x, y)
        {
            FixationInfo = fixationInfo;
        }

        public string FixationInfo { get; set; }
    }
}
