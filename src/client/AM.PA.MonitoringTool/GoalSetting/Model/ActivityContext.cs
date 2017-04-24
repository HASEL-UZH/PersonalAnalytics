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
        /// <summary>
        /// Start of this activity
        /// </summary>
        public DateTime Start { get; set; }

        /// <summary>
        /// End of this activity
        /// </summary>
        public DateTime? End { get; set; }

        /// <summary>
        /// Type of activity
        /// </summary>
        public ContextCategory Activity { get; set; }

        /// <summary>
        /// Duration. Difference beetween start and end.
        /// </summary>
        public TimeSpan Duration
        {
            get
            {
                return End.HasValue ? End.Value - Start : TimeSpan.FromMilliseconds(0);
            }
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return End.HasValue ? Activity + " from " + Start.ToString(Settings.DateFormat) + " to " + End.Value.ToString(Settings.DateFormat) : Activity + " from " + Start.ToString(Settings.DateFormat) + " to " + "N/A";
        }

        /// <summary>
        /// Returns true if the activity type (e.g. DevCode) is the same
        /// </summary>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (! (obj is ActivityContext))
            {
                return false;
            } else
            {
                return (obj as ActivityContext).Activity.ToString().Equals(this.Activity.ToString());
            }
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return 13 * Activity.GetHashCode();
        }

    }

}