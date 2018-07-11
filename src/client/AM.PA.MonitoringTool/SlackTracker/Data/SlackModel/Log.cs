// Created by Rohit Kaushik (f20150115@goa.bits-pilani.ac.in) at the University of Zurich
// Created: 2018-07-09
// 
// Licensed under the MIT License.

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
        public DateTime timestamp { get; set; }
    }
}
