using SQLiteNetExtensions.Attributes;

namespace EyeCatcherDatabase.Records
{
    public class DesktopWindowLinkRecord : Record
    {
        [ManyToOne]
        public DesktopRecord Desktop { get; set; }

        [ForeignKey(typeof(DesktopRecord))]
        public int DesktopId { get; set; }

        [ManyToOne(CascadeOperations = CascadeOperation.All)]
        public WindowRecord Window { get; set; }

        [ForeignKey(typeof(WindowRecord))]
        public int WindowId { get; set; }

        public int ZIndex { get; set; }

        public override string ToString()
        {
            return $"D: {DesktopId:X}, Z:{ZIndex}, W:{Window}";
        }
    }
}
