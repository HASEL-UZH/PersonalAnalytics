using System;
using Shared.Data;
using GoalSetting.Model;
using System.Collections.Generic;

namespace GoalSetting
{
    internal class Activity
    {
        public ContextCategory Category { get; internal set; }
        public TimeSpan TotalDuration { get; internal set; }
        public int TotalSwitches { get; internal set; }
        public List<ActivityContext> Context { get; internal set; }

        public override string ToString()
        {
            return Category.ToString() + ": " + TotalDuration.ToString() + " / " + TotalSwitches + " / " + Context.Count;
        }
    }
}