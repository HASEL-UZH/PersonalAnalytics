using EyeCatcherDatabase.Enums;
using SQLiteNetExtensions.Attributes;

namespace EyeCatcherDatabase.Records
{
    /// <summary>
    /// Describing how to get from one record to another
    /// </summary>
    public class DesktopTransitionRecord : Record
    {
        [ForeignKey(typeof(DesktopRecord))]
        public int DesktopId { get; set; }

        public DesktopTransitionType TransitionType { get; set; }

        [ManyToOne]
        public WindowRecord Window { get; set; }

        [ForeignKey(typeof(WindowRecord))]
        public int WindowId { get; set; }
    }
}
