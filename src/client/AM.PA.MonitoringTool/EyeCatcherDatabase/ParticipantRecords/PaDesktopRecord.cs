using EyeCatcherDatabase.Records;

namespace EyeCatcherDatabase.ParticipantRecords
{
    public class PaDesktopRecord : DesktopRecord, IParticipantRecord
    {
        public int ParticipantId { get; set; }
    }
}
