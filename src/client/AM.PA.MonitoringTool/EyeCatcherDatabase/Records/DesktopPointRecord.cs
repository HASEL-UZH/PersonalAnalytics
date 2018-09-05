using System.Drawing;
using EyeCatcherDatabase.Enums;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EyeCatcherDatabase.Records
{
    public class DesktopPointRecord : Record
    {
        public DesktopPointType Type { get; set; }

        [Ignore]
        public Point Point
        {
            get => new Point(X, Y);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        public int X { get; set; }
        public int Y { get; set; }

        [ManyToOne]
        public WindowRecord Window { get; set; }

        [ForeignKey(typeof(WindowRecord))]
        public int WindowId { get; set; }

        public string FixationInfo { get; set; }
        public string AdditionalInfo { get; set; }

    }
}
