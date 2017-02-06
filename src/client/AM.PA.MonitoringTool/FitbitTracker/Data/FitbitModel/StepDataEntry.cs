// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-26
// 
// Licensed under the MIT License.


using System;

namespace FitbitTracker.Data.FitbitModel
{

    public class StepDataEntry
    {

        private double steps = double.NaN;

        public DateTime Time { get; set; }

        public double Value { get { return steps; } set { steps = value; } }

    }

}