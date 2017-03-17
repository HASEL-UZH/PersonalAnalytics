// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-25
// 
// Licensed under the MIT License.

using System;

namespace FitbitTracker.Data.FitbitModel
{

    public class Device
    {

        public string Battery { get; set;  }

        public string DeviceVersion { get; set; }

        public string Id { get; set; }

        public DateTime LastSyncTime { get; set; }

        public string Type { get; set; }
        
        public string MAC { get; set; }
        
    }

}