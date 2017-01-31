// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-16
// 
// Licensed under the MIT License.

namespace BluetoothLowEnergyConnector
{
    internal class Settings
    {
        internal static double TIME_SINCE_NO_DATA_RECEIVED = 15;
        internal static int WAIT_TIME_BETWEEN_RECONNECT_TRIES = 30 * 1000;
        internal static int WAIT_TIME_BETWEEN_WATCHING_THREADS = 15 * 1000;
    }
}