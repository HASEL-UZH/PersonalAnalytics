using System.Collections.Generic;
using SQLiteNetExtensions.Attributes;

namespace EyeCatcherDatabase.Records
{
    public class DesktopRecord : Record
    {
        // ReSharper disable once CollectionNeverQueried.Global
        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<DesktopWindowLinkRecord> WindowLinks { get; set; }

        [ForeignKey(typeof(DesktopTransitionRecord))]
        public int TransitionId { get; set; }

        [OneToOne(CascadeOperations = CascadeOperation.All)]
        public DesktopTransitionRecord Transition { get; set; }
    }
}
