// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.


using Shared.Data;
using System;

namespace GoalSetting.Model
{
    public class ActivityContext
    {
        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public ContextCategory Activity { get; set; }
    }

}