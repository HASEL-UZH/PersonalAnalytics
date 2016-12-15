﻿// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using System;

namespace BluetoothLowEnergy
{
    public class HeartRateMeasurement
    {
        private double hr = Double.NaN;

        public double HeartRateValue { get { return hr; } set { hr = value; } }
        public string Timestamp { get; set; }
        public double RRInterval { get; set;  }

        public override string ToString()
        {
            return HeartRateValue.ToString() + " bpm / " + RRInterval.ToString() + " ms @ " + Timestamp;
        }
    }
}