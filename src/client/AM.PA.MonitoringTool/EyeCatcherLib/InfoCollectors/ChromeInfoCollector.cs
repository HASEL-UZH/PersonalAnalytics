using System;
using EyeCatcherDatabase.Records;

namespace EyeCatcherLib.InfoCollectors
{
    internal class ChromeInfoCollector : ApplicationInfoCollector
    {
        public override string ProcessName { get; } = "chrome";
        private const int HeightOfTabBar = 47;

        public override void AdjustApplicationInfo(DesktopPointRecord desktopPointRecord)
        {
            if (desktopPointRecord.Window.HWnd == IntPtr.Zero)
            {
                return;
            }
            if (desktopPointRecord.Window.Title == "Chrome Legacy Window")
            {
                return;
            }
            if (desktopPointRecord.Y - desktopPointRecord.Window.Rectangle.Top < HeightOfTabBar)
            {
                desktopPointRecord.AdditionalInfo = "Tab bar";
            }
        }
    }
}
