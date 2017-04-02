// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-27
// 
// Licensed under the MIT License.

using GoalSetting.Model;
using System;

namespace GoalSetting.Rules
{
    public class Rule
    {
        //GOAL
        public Goal Goal { get; set; }

        public string GoalString { get { return Goal.ToString(); } }


        //OPERATOR
        public Operator Operator { get; set; }

        public string OperatorString { get { return Operator.ToString();  } }

        private string _target;

        //That's milliseconds!
        public string TargetValue { get { return _target; } set {
                _target = value;
                if (Goal == Goal.TimeSpentOn)
                {
                    TimeSpan time = TimeSpan.FromMilliseconds(Convert.ToDouble(value));
                    if (time.TotalMinutes < 60)
                    {
                        if (time.TotalMinutes == 1)
                        {
                            _timespan = time.TotalMinutes + " minute";
                        }
                        else
                        {
                            _timespan = time.TotalMinutes + " minutes";
                        }
                    }
                    else if (time.TotalHours < 24)
                    {
                        if (time.TotalHours == 1)
                        {
                            _timespan = time.TotalHours + " hour";
                        }
                        else
                        {
                            _timespan = time.TotalHours + " hours";
                        }
                    }
                    else
                    {
                        if (time.TotalDays == 1)
                        {
                            _timespan = time.TotalDays + " day";
                        }
                        else
                        {
                            _timespan = time.TotalDays + " days";
                        }
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