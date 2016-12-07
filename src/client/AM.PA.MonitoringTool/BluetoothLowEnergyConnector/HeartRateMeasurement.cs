using System;

namespace BluetoothGattHeartRate
{
    public class HeartRateMeasurement
    {
        public ushort HeartRateValue { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public override string ToString()
        {
            return HeartRateValue.ToString() + " bpm @ " + Timestamp.ToString();
        }
    }
}
