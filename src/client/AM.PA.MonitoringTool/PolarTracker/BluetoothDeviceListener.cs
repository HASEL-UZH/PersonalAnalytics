namespace PolarTracker
{
    public interface BluetoothDeviceListener
    {

        void OnConnectionEstablished(string deviceName);

        void OnTrackerDisabled();
    }
}