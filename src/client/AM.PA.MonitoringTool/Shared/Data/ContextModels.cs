// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using System.ComponentModel;

namespace Shared.Data
{
    public enum ContextCategory
    {
        [Description("coding")]
        DevCode,

        [Description("debugging")]
        DevDebug,

        [Description("reviewing")]
        DevReview,

        [Description("versioning control")]
        DevVc,

        [Description("email")]
        Email,

        [Description("planning")]
        Planning,

        [Description("reading/writing")]
        ReadWriteDocument,

        [Description("planned meetings")]
        PlannedMeeting,

        [Description("informal meetings")]
        InformalMeeting,

        [Description("work-related browsing")]
        WorkRelatedBrowsing,

        [Description("work-unrelated browsing")]
        WorkUnrelatedBrowsing,

        Other,
        OtherRdp,
        Unknown,
        None // break or not on PC
    }

    public class ContextInfos
    {
        /// <summary>
        /// used for visualization
        /// </summary>
        public ContextCategory Category { get; set; }
        /// <summary>
        /// used to map activity
        /// </summary>
        public string ProgramInUse { get; set; } 
        /// <summary>
        /// used to more accurately map activity
        /// </summary>
        public string WindowTitle { get; set; }
        /// <summary>
        /// might be used to identify work unrelated/related stuff
        /// and more accurately map activity
        /// maybe tasks too in the future
        /// </summary>
        public string WindowContent { get; set; }

        public int UserInputLevel { get; set; }

        // in the future: maybe add meetings
    }
}
