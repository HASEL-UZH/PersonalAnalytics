using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackTracker.Data.SlackModel
{
    class LogData
    {
        public int Id {get; set;}
        public DateTime Timestamp { get; set; }
        public string ChannelId;
        public string Author { get; set; }
        public List<string> Mentions { get; set; }
        public string Message { get; set; }
        public int Cluster_id { get; set; }
    }
}
