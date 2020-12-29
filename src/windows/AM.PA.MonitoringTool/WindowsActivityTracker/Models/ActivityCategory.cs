// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-04
// 
// Licensed under the MIT License.

namespace WindowsActivityTracker.Models
{
    /// <summary>
    /// Activitiy categories which are automatically mapped for the retrospection
    /// by the ProcessToActivityMapper
    /// </summary>
    public enum ActivityCategory
    {
        Uncategorized, // default
        DevCode,
        DevDebug,
        DevReview,
        DevVc,
        Email,
        Planning,
        ReadWriteDocument,
        PlannedMeeting,
        InformalMeeting,
        InstantMessaging, // subcategory of InformalMeeting
        //WebBrowsing, // uncategorized web browsing
        WorkRelatedBrowsing,
        WorkUnrelatedBrowsing,
        FileNavigationInExplorer,
        Other,
        OtherRdp,
        Idle, // all IDLE events that can't be mapped elsewhere
        Unknown
    }
}
