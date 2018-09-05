using System;
using System.Collections.Generic;
using System.Linq;
using EyeCatcherDatabase;
using EyeCatcherDatabase.Enums;
using EyeCatcherDatabase.Records;
using EyeCatcherLib.InfoCollectors;
using EyeCatcherLib.Native;
using EyeCatcherLib.Providers;
using EyeCatcherLib.Utils;

namespace EyeCatcherLib
{
    public class DesktopPointRecordProvider
    {
        private readonly IWindowRecordProvider _windowRecordProvider;

        public DesktopPointRecordProvider(IWindowRecordProvider windowRecordProvider)
        {
            _windowRecordProvider = windowRecordProvider ?? throw new ArgumentNullException(nameof(windowRecordProvider));
            InfoCollectors = ReflectionUtils.GetAndActivateInstances<ApplicationInfoCollector>();
        }

        private IList<ApplicationInfoCollector> InfoCollectors { get; }

        /// <summary>
        /// Returns the <see cref="DesktopPointRecord"/> of a Point
        /// </summary>
        public DesktopPointRecord GetDesktopPointInfo(PointTime pointTime, DesktopPointType type)
        {
            var hWnd = NativeMethods.WindowFromPoint(pointTime.Point);
            if (hWnd == IntPtr.Zero)
            {
                return new DesktopPointRecord
                {
                    Timestamp = pointTime.Time,
                    Type = type,
                    Point = pointTime.Point,
                    FixationInfo = (pointTime as FixationPointTime)?.FixationInfo
                };
            }

            var windowRecord = _windowRecordProvider.GetWindowRecord(hWnd);
            if (windowRecord == null)
            {
                // Try parent
                var parentHwnd = NativeMethods.GetTopLevelParent(hWnd);
                windowRecord = _windowRecordProvider.GetWindowRecord(parentHwnd);
            }
            var desktopPointInfo = new DesktopPointRecord
            {
                Timestamp = pointTime.Time,
                Type = type,
                Point = pointTime.Point,
                FixationInfo = (pointTime as FixationPointTime)?.FixationInfo,
                Window = windowRecord // might be null
            };

            var collector = InfoCollectors.FirstOrDefault(infoCollector => infoCollector.IsResponsible(desktopPointInfo));
            collector?.AdjustApplicationInfo(desktopPointInfo);
            return desktopPointInfo;
        }
    }
}
