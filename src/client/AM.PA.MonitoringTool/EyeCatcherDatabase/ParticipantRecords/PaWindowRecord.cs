using EyeCatcherDatabase.Records;

namespace EyeCatcherDatabase.ParticipantRecords
{
    public class PaWindowRecord : WindowRecord, IParticipantRecord
    {
        public int ParticipantId { get; set; }
    }
}
