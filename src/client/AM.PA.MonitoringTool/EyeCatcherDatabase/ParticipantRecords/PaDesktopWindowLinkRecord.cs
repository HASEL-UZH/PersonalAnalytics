using EyeCatcherDatabase.Records;

namespace EyeCatcherDatabase.ParticipantRecords
{
    public class PaDesktopWindowLinkRecord : DesktopWindowLinkRecord, IParticipantRecord
    {
        public int ParticipantId { get; set; }
    }
}
