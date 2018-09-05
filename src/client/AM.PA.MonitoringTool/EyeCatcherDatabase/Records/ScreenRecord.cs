using System;
using System.Drawing;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EyeCatcherDatabase.Records
{
    [Serializable]
    public class ScreenRecord : Record
    {
        public string Name { get; set; }
        [Ignore]
        public IntPtr HMonitor { get; set; }
        public long HMonitorInteger { get; set; }
        public bool IsPrimary { get; set; }

        [Ignore]
        public Rectangle Bounds
        {
            get => new Rectangle(BoundsX, BoundsY, BoundsWidth, BoundsHeight);
            set
            {
                BoundsX = value.X;
                BoundsY = value.Y;
                BoundsWidth = value.Width;
                BoundsHeight = value.Height;
            }
        }

        // Bounds
        public int BoundsX { get; set; }
        public int BoundsY { get; set; }
        public int BoundsWidth { get; set; }
        public int BoundsHeight { get; set; }

        [Ignore]
        public Rectangle WorkingArea
        {
            get => new Rectangle(WorkingAreaX, WorkingAreaY, WorkingAreaWidth, WorkingAreaHeight);
            set
            {
                WorkingAreaX = value.X;
                WorkingAreaY = value.Y;
                WorkingAreaWidth = value.Width;
                WorkingAreaHeight = value.Height;
            }
        }

        // WorkingArea
        public int WorkingAreaX { get; set; }
        public int WorkingAreaY { get; set; }
        public int WorkingAreaWidth { get; set; }
        public int WorkingAreaHeight { get; set; }

        [Ignore]
        public Rectangle TaskBar
        {
            get => new Rectangle(TaskBarX, TaskBarY, TaskBarWidth, TaskBarHeight);
            set
            {
                TaskBarX = value.X;
                TaskBarY = value.Y;
                TaskBarWidth = value.Width;
                TaskBarHeight = value.Height;
            }
        }

        // Taskbar
        public int TaskBarX { get; set; }
        public int TaskBarY { get; set; }
        public int TaskBarWidth { get; set; }
        public int TaskBarHeight { get; set; }

        [ForeignKey(typeof(ScreenLayoutRecord))]
        public int ScreenLayoutId { get; set; }

    }
}
