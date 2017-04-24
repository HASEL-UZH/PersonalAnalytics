// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

using GoalSetting.Model;
using System;

namespace GoalSetting.Goals
{
    public abstract class Goal
    {
        /// <summary>
        /// Unique ID for each goal. The unique ID is generated when a new goal is added to the database.
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        /// The title of this goal. The user can specific this while creating a new goal.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The rule associated with this goal.
        /// </summary>
        public Rule Rule { get; set; }
        
        /// <summary>
        /// True if the goal should be used in the retrospection visualization, false otherwise
        /// </summary>
        public bool IsVisualizationEnabled { get; set; }
        
        private Progress _progress = null;

        /// <summary>
        /// Stores the progress an user has made towards this goal. The progress can also be null if the rule has never been checked.
        /// </summary>
        public Progress Progress { get { if (_progress == null) { _progress = new Progress(); } return _progress; } set { _progress = value; } }

        /// <summary>
        /// An action that should be performed when the user does not achieve / violates this goal. This is not yet implemented!
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// A string representation of the point in time when this rule should be checked
        /// </summary>
        public string When { get; set; }

        /// <summary>
        /// Stores the compiled rule associated with this goal
        /// </summary>
        internal Func<Activity, bool> CompiledRule { get; set; }

        /// <summary>
        /// Complies the rule associated with this goal
        /// </summary>
        public abstract void Compile();

        /// <summary>
        /// Calculates the progress an user has made towards this goal. The progress is stored in the 'Progress' property.
        /// The boolean passed in the parameter indicates whether the progress should be stored in the database.
        /// </summary>
        /// <param name="persist"></param>
        public abstract void CalculateProgressStatus(bool persist);

        /// <summary>
        /// Returns a message that describes the progress the user has made towards this goal.
        /// </summary>
        /// <returns></returns>
        public abstract string GetProgressMessage();

        /// <summary>
        /// Indicates whether an user can still reach this goal.
        /// </summary>
        /// <returns></returns>
        public abstract bool IsStillReachable();   
    }
    
    public class Progress
    {
        public ProgressStatus Status { get; set; }
        public bool? Success { get; set; }
        public string Time { get; set; }
        public int Switches { get; set; }
        public double Actual { get; set; }
        public double Target { get; set; }
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