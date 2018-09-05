using EyeCatcherDatabase.Records;

namespace EyeCatcherDatabase.ParticipantRecords
{
    public interface IParticipantRecord : IRecord
    {
        int ParticipantId { get; set; }
    }
}