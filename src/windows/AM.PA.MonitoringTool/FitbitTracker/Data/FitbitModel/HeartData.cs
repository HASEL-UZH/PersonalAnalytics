// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-25
// 
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace FitbitTracker.Data.FitbitModel
{

    class HeartData
    {

        [JsonProperty("activities-heart")]
        public List<HeartActivities> Activities { get; set; }

        [JsonProperty("activities-heart-intraday")]
        public HeartActivitiesIntraday IntradayActivities { get; set; }

    }

}