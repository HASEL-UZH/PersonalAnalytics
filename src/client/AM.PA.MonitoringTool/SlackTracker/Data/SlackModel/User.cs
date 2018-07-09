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
        public bool is_admin { get; set; }
        public bool is_owner { get; set; }
        public bool is_bot { get; set; }

    }
}
