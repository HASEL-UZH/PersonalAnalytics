using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackTracker.Data.SlackModel
{
    class UserActivity
    {
        public string channel_id { get; set; }
        public string from { get; set; }
        public string to { get; set; }
        public List<string> words { get; set; }
        public DateTime start_time { get; set; }
        public DateTime end_time { get; set; }
    }
}
