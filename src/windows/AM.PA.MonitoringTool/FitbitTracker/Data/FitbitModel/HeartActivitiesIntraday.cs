// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-26
// 
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace FitbitTracker.Data.FitbitModel
{

    public class HeartActivitiesIntraday
    {

        [JsonProperty("dataset")]
        public List<HeartrateIntradayData> HeartrateIntradayData { get; set; }

    }

}