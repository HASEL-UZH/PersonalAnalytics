using EyeCatcherDatabase.Records;

namespace EyeCatcherDatabase.ParticipantRecords
{
    public class PaEyePositionRecord : EyePositionRecord, IParticipantRecord
    {
        public int ParticipantId { get; set; }
    }
}
