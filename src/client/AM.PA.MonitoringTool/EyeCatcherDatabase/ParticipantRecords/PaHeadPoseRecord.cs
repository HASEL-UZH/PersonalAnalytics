using EyeCatcherDatabase.Records;

namespace EyeCatcherDatabase.ParticipantRecords
{
    public class PaHeadPoseRecord : ScreenRecord, IParticipantRecord
    {
        public int ParticipantId { get; set; }
    }
}
