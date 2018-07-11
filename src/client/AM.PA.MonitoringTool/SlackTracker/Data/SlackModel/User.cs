// Created by Rohit Kaushik (f20150115@goa.bits-pilani.ac.in) at the University of Zurich
// Created: 2018-07-09
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace SlackTracker.Data.SlackModel
{
    class User
    {
        public string id { get; set; }
        public string team_id { get; set; }
        public string name { get; set; }
        public string real_name { get; set; }
        public bool is_bot { get; set; }
    }
}
