using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackTracker.Data.SlackModel
{
    class LogData
    {
        public int id {get; set;}
        public DateTime timestamp { get; set; }
        public string channel_id;
        public string author { get; set; }
        public List<string> mentions { get; set; }
        public string message { get; set; }
        public int cluster_id { get; set; }
    }
}
