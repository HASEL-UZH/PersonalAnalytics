using EyeCatcherDatabase.Records;

namespace EyeCatcherDatabase.ParticipantRecords
{
    public class PaDesktopPointRecord : DesktopPointRecord, IParticipantRecord
    {
        public int ParticipantId { get; set; }
    }
}
