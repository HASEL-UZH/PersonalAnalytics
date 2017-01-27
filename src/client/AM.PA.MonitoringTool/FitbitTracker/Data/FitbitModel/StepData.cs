// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-26
// 
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace FitbitTracker.Data.FitbitModel
{

    class StepData
    {

        [JsonProperty("activities-steps-intraday")]
        public StepDataIntraday IntraDay { get; set; }

    }

}