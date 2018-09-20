using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SlackTracker.Data.SlackModel
{
    class Thread
    {
        public int id { get; set; }
        public List<LogData> messages { get; set; }
        public DateTime start_time { get; set; }
        public DateTime end_time { get; set; }
        public List<string> user_participated { get; set; }
        public List<string> keywords { get; set; }

        public Thread()
        {
            messages = new List<LogData>();
            user_participated = new List<string>();
            keywords = new List<string>();
        }
    }
}
