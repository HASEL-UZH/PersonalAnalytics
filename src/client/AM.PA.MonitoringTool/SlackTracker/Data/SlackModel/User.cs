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
        public string Id { get; set; }
        public string TeamId { get; set; }
        public string Name { get; set; }
        public string RealName { get; set; }
        public bool IsBot { get; set; }
    }
}
