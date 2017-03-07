// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-27
// 
// Licensed under the MIT License.

using System;

namespace GoalSetting.Rules
{
    public class Rule
    {
        public string Goal { get; set; }

        public string Operator { get; set; }

        private string _target;

        //That's milliseconds!
        public string TargetValue { get { return _target; } set {
                _target = value;
                if (Goal.Equals(Model.Goal.TimeSpentOn.ToString()))
                {
                    TimeSpan time = TimeSpan.FromMilliseconds(Convert.ToDouble(value));
                    if (time.TotalMinutes < 60)
                    {
                        _timespan = time.TotalMinutes + " minutes";
                    }
                    else if (time.TotalHours < 24)
                    {
                        _timespan = time.TotalHours + " hours";
                    }
                    else
                    {
                        _timespan = time.TotalDays + " days";
                    }
                }
                else
                {
                    _timespan = value;
                }
            }
        }

        private string _timespan;

        public string TargetTimeSpan { get { return _timespan; } }
    }
}