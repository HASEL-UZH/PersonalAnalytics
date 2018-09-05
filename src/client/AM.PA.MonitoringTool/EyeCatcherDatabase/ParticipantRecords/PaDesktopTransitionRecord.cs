using EyeCatcherDatabase.Records;

namespace EyeCatcherDatabase.ParticipantRecords
{
    public class PaDesktopTransitionRecord : DesktopTransitionRecord, IParticipantRecord
    {
        public int ParticipantId { get; set; }
    }
}
