// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-03
// 
// Licensed under the MIT License.

using Newtonsoft.Json;
using System;

namespace FitbitTracker.Model
{
    public class SleepIntraData
    {
        [JsonProperty("dateTime")]
        public DateTime Time { get; set; }
        public int Value { get; set; }
    }
}