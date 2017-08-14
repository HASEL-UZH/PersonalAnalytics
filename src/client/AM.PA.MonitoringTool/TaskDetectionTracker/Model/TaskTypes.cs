// Created by Andre Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2017-08-14
// 
// Licensed under the MIT License.


using System.ComponentModel;

namespace TaskDetectionTracker.Model
{
    public enum TaskTypes
    {
        //[Description("Other")]
        Other,
        //[Description("Private")]
        Private,
        //[Description("Planned Meeting")]
        PlannedMeeting,
        //[Description("Ad-hoc Meeting")]
        UnplannedMeeting,
        //[Description("Awareness & Team")]
        Awareness,
        //[Description("Planning")]
        Planning,
        //[Description("Study")]
        Observation,
        //[Description("Development")]
        Development,
        //[Description("Administrative Work")]
        AdminstrativeWork
    }
}
