using EyeCatcherDatabase.Records;

namespace EyeCatcherDatabase.ParticipantRecords
{
    public class PaUserPresenceRecord : UserPresenceRecord, IParticipantRecord
    {
        public int ParticipantId { get; set; }
    }
}
