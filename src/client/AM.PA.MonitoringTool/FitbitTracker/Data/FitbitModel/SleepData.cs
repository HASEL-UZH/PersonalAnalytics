// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-23
// 
// Licensed under the MIT License.

using System.Collections.Generic;

namespace FitbitTracker.Model
{

    class SleepData
    {
        public List<SleepLog> Sleep { get; set;  }
        public SleepSummary Summary { get; set;  }
    }

}