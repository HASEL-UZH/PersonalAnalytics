using System;
using EyeCatcherDatabase.Records;

namespace EyeCatcherLib.InfoCollectors
{
    internal class Windows10AppInfoCollector : ApplicationInfoCollector
    {
        public override string ProcessName { get; } = "ApplicationFrameHost";

        public override void AdjustApplicationInfo(DesktopPointRecord desktopPointRecord)
        {
            if (string.IsNullOrWhiteSpace(desktopPointRecord.Window.Title))
            {
                return;
            }

            var lastDash = desktopPointRecord.Window.Title.LastIndexOf("- ", StringComparison.Ordinal);
            if (lastDash <= 0)
            {
                desktopPointRecord.Window.ProcessName = desktopPointRecord.Window.Title;
                return;
            }

            var processName = desktopPointRecord.Window.Title.Substring(lastDash).Replace("- ", "").Trim();
            if (!string.IsNullOrEmpty(processName))
            {
                desktopPointRecord.Window.ProcessName = processName;
            }
        }
    }
}
