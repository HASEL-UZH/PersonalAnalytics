using EyeCatcherDatabase.Records;

namespace EyeCatcherDatabase.ParticipantRecords
{
    public class PaScreenRecord : ScreenRecord, IParticipantRecord
    {
        public int ParticipantId { get; set; }
    }
}
