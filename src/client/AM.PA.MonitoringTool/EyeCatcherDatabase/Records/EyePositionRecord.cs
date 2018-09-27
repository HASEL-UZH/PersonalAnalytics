namespace EyeCatcherDatabase.Records
{
    public class EyePositionRecord : Record
    {
        public double LeftEyeX { get; set; }
        public double LeftEyeY { get; set; }
        public double LeftEyeZ { get; set; }

        public double LeftEyeNormalizedX { get; set; }
        public double LeftEyeNormalizedY { get; set; }
        public double LeftEyeNormalizedZ { get; set; }


        public double RightEyeX { get; set; }
        public double RightEyeY { get; set; }
        public double RightEyeZ { get; set; }

        public double RightEyeNormalizedX { get; set; }
        public double RightEyeNormalizedY { get; set; }
        public double RightEyeNormalizedZ { get; set; }
    }
}
