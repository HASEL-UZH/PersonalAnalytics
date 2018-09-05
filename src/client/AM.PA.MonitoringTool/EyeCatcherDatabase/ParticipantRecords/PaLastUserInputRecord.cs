using EyeCatcherDatabase.Records;

namespace EyeCatcherDatabase.ParticipantRecords
{
    public class PaLastUserInputRecord : LastUserInputRecord, IParticipantRecord
    {
        public int ParticipantId { get; set; }
    }
}
