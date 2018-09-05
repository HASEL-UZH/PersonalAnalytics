using EyeCatcherDatabase.Records;

namespace EyeCatcherLib.InfoCollectors
{
    /// <summary>
    /// Collects information about a specific application
    /// </summary>
    internal abstract class ApplicationInfoCollector
    {
        public abstract string ProcessName { get; }

        public virtual bool IsResponsible(DesktopPointRecord desktopPointRecord)
        {
            if (desktopPointRecord.Window == null)
            {
                return false;
            }
            return ProcessName == desktopPointRecord.Window.ProcessName;
        }

        public abstract void AdjustApplicationInfo(DesktopPointRecord desktopPointRecord);
    }
}
