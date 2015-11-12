// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

namespace TaskSwitchTracker
{
    public class Settings
    {
        public const bool IsEnabled = false;

        public const int Interval = 60000; //ms
        public const string DbTable = "user_task_switches";

        public const int ButtonWindowWidth = 20;
        public const int ButtonWindowHeight = 20;
    }
}
