// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-27
// 
// Licensed under the MIT License.

using System;

namespace GoalSetting.Model
{
    public class Rule
    {
        //GOAL
        public RuleGoal Goal { get; set; }

        //GOAL (string representation)
        public string GoalString { get { return Goal.ToString(); } }


        //OPERATOR
        public RuleOperator Operator { get; set; }

        //OPERATOR (string representation)
        public string OperatorString { get { return Operator.ToString();  } }
        
        private string _target;
    
        //TARGET (string representation)    
        public string TargetValue { get { return _target; } set {
                _target = value;
                if (Goal == RuleGoal.TimeSpentOn)
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

        //TARGET TIMESPAN (string representation)
        public string TargetTimeSpan { get { return _timespan; } }
    }
}