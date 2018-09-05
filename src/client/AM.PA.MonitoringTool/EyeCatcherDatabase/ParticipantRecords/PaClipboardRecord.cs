using EyeCatcherDatabase.Records;

namespace EyeCatcherDatabase.ParticipantRecords
{
    public class PaClipboardRecord : CopyPasteRecord, IParticipantRecord
    {
        public int ParticipantId { get; set; }
    }
}
