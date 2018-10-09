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
    class Channel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public long Created { get; set; }
        public string Creator { get; set; }
    }
}
