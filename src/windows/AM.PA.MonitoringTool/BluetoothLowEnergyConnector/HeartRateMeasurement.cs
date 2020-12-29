// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using System;

namespace BluetoothLowEnergy
{

    //Data class for storing measurement data from a BLE device.
    public class HeartRateMeasurement
    {
        private double _hr = Double.NaN;
        private double _rrDifference = Double.NaN;

        public double HeartRateValue { get { return _hr; } set { _hr = value; } }
        public string Timestamp { get; set; }
        public double RRInterval { get; set;  }
        public double RRDifference { get { return _rrDifference; } set { _rrDifference = value; }  }

        public override string ToString()
        {
            return HeartRateValue.ToString() + " bpm / " + RRInterval.ToString() + " ms / " + RRDifference.ToString() + " @ " + Timestamp;
        }

    }
}