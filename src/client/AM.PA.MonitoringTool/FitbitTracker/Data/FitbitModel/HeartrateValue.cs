// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-25
// 
// Licensed under the MIT License.

using System.Collections.Generic;

namespace FitbitTracker.Data.FitbitModel
{

    public class HeartrateValue
    {

        public int RestingHeartrate { get; set; }

        public List<HeartRateZone> CustomHeartrateZones { get; set; }

        public List<HeartRateZone> HeartRateZones { get; set; }

    }

}