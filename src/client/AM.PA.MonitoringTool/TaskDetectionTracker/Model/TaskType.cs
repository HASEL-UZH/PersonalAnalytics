// Created by Andre Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2017-08-14
// 
// Licensed under the MIT License.

using System.ComponentModel;

namespace TaskDetectionTracker.Model
{
    public enum TaskType
    {
        [Description("Other (please specify in the comments field)")]
        Other,
        [Description("Private (breaks, lunch, work-unrelated webbrowsing, private phone calls)")]
        Private,
        [Description("Planned Meeting")]
        PlannedMeeting,
        [Description("Ad-hoc or unplanned Meeting")]
        UnplannedMeeting,
        [Description("Awareness & Team (e.g. quick chat/email)")]
        Awareness,
        [Description("Planning (scheduling meetings, managing tasks)")]
        Planning,
        [Description("Study/Observation (activities related to this study)")]
        Observation,
        [Description("Development (implementing feature, fixing bugs, code reviews, testing)")]
        Development,
        [Description("Administrative work (e.g. connects)")]
        AdminstrativeWork
    }
}
