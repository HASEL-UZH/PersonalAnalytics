using System.Collections.Generic;

namespace FitbitTracker.Model
{

    class SleepData
    {
        public List<SleepLog> Sleep { get; set;  }
        public SleepSummary Summary { get; set;  }
    }

}