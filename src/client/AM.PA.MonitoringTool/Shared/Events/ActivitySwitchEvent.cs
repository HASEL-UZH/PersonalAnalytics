// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

namespace Shared.Events
{
    public class ActivitySwitchEvent : TrackerEvents
    {
        public string NewWindowTitle { get; set; }
        public string NewProcessName { get; set; }
    }
}