using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using EyeCatcherDatabase.Records;
using EyeCatcherLib.Native;

namespace EyeCatcherLib
{
    public class ScreenLayoutRecordProvider : IScreenLayoutRecordProvider, IGetScreenRecords
    {
        private enum TaskBarPosition
        {
            Top,
            Bottom,
            Left,
            Right
        }

        public IList<ScreenRecord> CurrentScreens => CurrentScreenLayoutRecord.Screens;

        public ScreenLayoutRecord CurrentScreenLayoutRecord { get; private set; }

        public ScreenLayoutRecord GetScreenLayout()
        {
            var monitorInfos = NativeMethods.GetAllMonitors();
            CurrentScreenLayoutRecord = new ScreenLayoutRecord
            {
                Screens = monitorInfos.Select(GetScreenRecord).ToList()
            };
            return CurrentScreenLayoutRecord;
        }

        private static ScreenRecord GetScreenRecord(IntPtr hMonitor)
        {
            var monitorInfoEx = NativeMethods.GetMonitorInfo(hMonitor);
            var bounds = (Rectangle) monitorInfoEx.rcMonitor;
            var workingArea = (Rectangle) monitorInfoEx.rcWork;
            return new ScreenRecord
            {
                Name = monitorInfoEx.DeviceName,
                HMonitor = hMonitor,
                HMonitorInteger = hMonitor.ToInt64(),
                IsPrimary = monitorInfoEx.dwFlags == 1,
                Bounds = bounds,
                WorkingArea = workingArea,
                TaskBar = GetTaskbar(bounds, workingArea),
            };
        }

        private static Rectangle GetTaskbar(Rectangle bounds, Rectangle workingArea)
        {
            var taskBarWidth = bounds.Width - workingArea.Width;
            var taskBarHeight = bounds.Height - workingArea.Height;

            if (taskBarWidth == 0 && taskBarHeight == 0)
            {
                // Taskbar Hidden
                return Rectangle.Empty;
            }
            if (taskBarWidth == 0)
            {
                var position = bounds.Top < workingArea.Top ? TaskBarPosition.Top : TaskBarPosition.Bottom;
                return GetTaskbar(bounds, workingArea, position, taskBarHeight);
            }
            if (taskBarHeight == 0)
            {
                var position = bounds.Left < workingArea.Left ? TaskBarPosition.Left : TaskBarPosition.Right;
                return GetTaskbar(bounds, workingArea, position, taskBarWidth);
            }
            // TODO RR: something does not match up.
            return Rectangle.Empty;
        }

        private static Rectangle GetTaskbar(Rectangle bounds, Rectangle workingArea, TaskBarPosition taskBarPosition, int taskBarThickness)
        {
            switch (taskBarPosition)
            {
                case TaskBarPosition.Top:
                    return new Rectangle(bounds.Left, bounds.Top, bounds.Width, taskBarThickness);
                case TaskBarPosition.Bottom:
                    return new Rectangle(bounds.Left, workingArea.Bottom, bounds.Width, taskBarThickness);
                case TaskBarPosition.Left:
                    return new Rectangle(bounds.Left, bounds.Top, taskBarThickness, bounds.Height);
                case TaskBarPosition.Right:
                    return new Rectangle(workingArea.Right, bounds.Top, taskBarThickness, bounds.Height);
                default:
                    throw new ArgumentOutOfRangeException(nameof(taskBarPosition), taskBarPosition, null);
            }
        }
    }
}
