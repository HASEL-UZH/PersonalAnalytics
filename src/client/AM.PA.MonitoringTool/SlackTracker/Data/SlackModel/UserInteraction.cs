using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackTracker.Data.SlackModel
{
    class UserInteraction
    {
        public string ChannelName { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public HashSet<string> Topics { get; set; }
        public DateTime Date { get; set; }
        public double Duration { get; set; }
    }
}
