// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

using System;

namespace GoalSetting.Rules
{
    public abstract class PARule
    {
        public string Title { get; set; }

        public Rule Rule { get; set; }
        
        public bool IsVisualizationEnabled { get; set; }
        
        private Progress _progress = null;

        public Progress Progress { get { if (_progress == null) { _progress = new Progress(); } return _progress; } set { _progress = value; } }

        public string Action { get; set; }

        public string When { get; set; }

        internal Func<Activity, bool> CompiledRule { get; set; }

        public abstract void Compile();

        public abstract void CalculateProgressStatus();

        public abstract string GetProgressMessage();
        
    }
    
    public class Progress
    {
        public ProgressStatus Status { get; set; }
        public bool? Success { get; set; }
        public string Time { get; set; }
        public int Switches { get; set; }
    }

    public enum ProgressStatus
    {
        VeryLow,
        Low,
        Average,
        High,
        VeryHigh
    }

}