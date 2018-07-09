using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackTracker.Data.SlackModel
{
    class Log
    {
        public string type { get; set; }
        public string sender { get; set; }
        public string receiver { get; set; }
        public string channel_id { get; set; }
        public string message { get; set; }
        public string timestamp { get; set; }
    }
}
