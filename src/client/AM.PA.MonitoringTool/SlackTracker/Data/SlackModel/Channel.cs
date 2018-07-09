using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackTracker.Data.SlackModel
{
    class Channel
    {
        public string id { get; set; }
        public string name { get; set; }
        public int created { get; set; }
        public string creator { get; set; }
    }
}
