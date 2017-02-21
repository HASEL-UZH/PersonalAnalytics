// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-16
// 
// Licensed under the MIT License.

namespace PolarTracker
{
    public interface BluetoothDeviceListener
    {
        void OnConnectionEstablished(string deviceName);

        void OnTrackerDisabled();
    }
}