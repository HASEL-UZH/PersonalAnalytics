using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EyeCatcherDatabase.Records
{
    [Serializable]
    public class WindowRecord : Record
    {

        #region Parent

        /// <summary>
        /// A Toplevel Window never has a ParentWindow
        /// </summary>
        [ManyToOne(nameof(ParentWindowId), nameof(ChildWindows), CascadeOperations = CascadeOperation.All)]
        public WindowRecord ParentWindow { get; set; }

        [OneToMany(nameof(ParentWindowId), nameof(ParentWindow), CascadeOperations = CascadeOperation.All)]
        public List<WindowRecord> ChildWindows { get; set; } = new List<WindowRecord>();

        [ForeignKey(typeof(WindowRecord))]
        public int ParentWindowId { get; set; }

        #endregion

        #region Owner

        public long OwnerHwndInteger { get; set; }

        #endregion

        #region Screen

        /// <summary>
        /// The Screen the Window is on (display monitor that has the largest area of intersection with the bounding rectangle of the window)
        /// </summary>
        [ManyToOne(CascadeOperations = CascadeOperation.All)]
        public ScreenRecord Screen { get; set; }

        [ForeignKey(typeof(ScreenRecord))]
        public int ScreenId { get; set; }

        #endregion

        #region General

        public string Title { get; set; }
        public string ClassName { get; set; }
        public string AdditionalInfo { get; set; }

        [Ignore]
        public IntPtr HWnd { get; set; }
        public long HwndInteger { get; set; }

        [Ignore]
        public Process Process { get; set; }
        public uint ProcessId { get; set; }
        public uint ThreadId { get; set; }
        public string ProcessName { get; set; }

        #endregion

        #region State

        public bool TopMostWindow { get; set; }
        public bool Minimized { get; set; }
        public bool Maximized { get; set; }

        public DateTime Activated { get; set; }

        [Ignore]
        public Rectangle Rectangle
        {
            get => new Rectangle(X, Y, Width, Height);
            set
            {
                X = value.X;
                Y = value.Y;
                Width = value.Width;
                Height = value.Height;
            }
        }

        // Bounds
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        #endregion

        #region Activity

        public int ActivityCategory { get; set; }

        public string ActivityName { get; set; }

        #endregion

        public override string ToString()
        {
            return $"Hwnd: {HwndInteger:X} {(string.IsNullOrWhiteSpace(Title) ? ProcessName : Title)} {Rectangle}";
        }

    }
}
