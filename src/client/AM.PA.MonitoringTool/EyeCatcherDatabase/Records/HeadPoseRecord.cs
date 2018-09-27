using System;

namespace EyeCatcherDatabase.Records
{
    /// <summary>
    /// Origin point of the coordinate system is at the center of the ‘track box’ (Display center) that contains the user head.
    /// </summary>
    [Serializable]
    public class HeadPoseRecord : Record
    {
        public double HeadPositionX { get; set; }
        public double HeadPositionY { get; set; }
        public double HeadPositionZ { get; set; }
        public double HeadRotationX { get; set; }
        public double HeadRotationY { get; set; }
        public double HeadRotationZ { get; set; }
    }
}
