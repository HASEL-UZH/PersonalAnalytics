using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackTracker.Data.SlackModel
{
    class UserInteraction
    {
        public string channel_id { get; set; }
        public string from { get; set; }
        public string to { get; set; }
        public List<string> topics { get; set; }
        public DateTime date { get; set; }
        public double duration { get; set; }
    }
}
