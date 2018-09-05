using EyeCatcherDatabase.Records;

namespace EyeCatcherDatabase.ParticipantRecords
{
    public class PaScreenLayoutRecord : ScreenLayoutRecord, IParticipantRecord
    {
        public int ParticipantId { get; set; }
    }
}
