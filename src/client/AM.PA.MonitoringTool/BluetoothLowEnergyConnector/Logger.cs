// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using System;

namespace BluetoothLowEnergy
{
    class Logger
    {
        internal static void WriteToConsole(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        internal static void WriteToConsole(Object obj)
        {
            WriteToConsole(obj.ToString());
        }
    }
}