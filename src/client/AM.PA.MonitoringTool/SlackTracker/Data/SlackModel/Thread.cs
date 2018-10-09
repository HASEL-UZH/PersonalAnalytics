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
        public int Id { get; set; }
        public List<LogData> Messages { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<string> UserParticipated { get; set; }
        public List<string> Keywords { get; set; }

        public Thread()
        {
            Messages = new List<LogData>();
            UserParticipated = new List<string>();
            Keywords = new List<string>();
        }
    }
}
