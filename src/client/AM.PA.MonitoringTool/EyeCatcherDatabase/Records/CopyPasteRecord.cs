using SQLiteNetExtensions.Attributes;

namespace EyeCatcherDatabase.Records
{
    public class CopyPasteRecord : Record
    {
        public string Url { get; set; }

        public string CopyContent { get; set; }

        [ManyToOne]
        public WindowRecord Window { get; set; }

        [ForeignKey(typeof(WindowRecord))]
        public int WindowId { get; set; }
    }
}
