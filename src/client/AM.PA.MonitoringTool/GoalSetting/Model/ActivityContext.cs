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

        public DateTime? End { get; set; }

        public ContextCategory Activity { get; set; }

        public TimeSpan Duration { get {
            if (End.HasValue)
                {
                    return End.Value - Start;
                }
                else
                {
                    return TimeSpan.FromMilliseconds(0);
                }
            }
        }

        public override string ToString()
        {
            if (End.HasValue)
            {
                return Activity + " from " + Start.ToString(Settings.DateFormat) + " to " + End.Value.ToString(Settings.DateFormat);
            }
            else
            {
                return Activity + " from " + Start.ToString(Settings.DateFormat) + " to " + "N/A";
            }
        }

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

        public override int GetHashCode()
        {
            return 13 * Activity.GetHashCode();
        }

    }

}