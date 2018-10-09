using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackTracker.Data.SlackModel
{
    class UserActivity
    {
        public DateTime Time { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public double Intensity { get; set; }
    }
}
