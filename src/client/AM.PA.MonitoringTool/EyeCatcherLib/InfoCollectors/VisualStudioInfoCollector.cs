using System;
using System.Drawing;
using System.Runtime.InteropServices;
using EyeCatcherDatabase.Records;

namespace EyeCatcherLib.InfoCollectors
{
    internal class VisualStudioInfoCollector : ApplicationInfoCollector
    {
        public override string ProcessName { get; } = "devenv";

        public override void AdjustApplicationInfo(DesktopPointRecord desktopPointRecord)
        {
            desktopPointRecord.AdditionalInfo = GetWindowCaptionForPoint(desktopPointRecord.Point);
        }

        public static string GetWindowCaptionForPoint(Point point)
        {
            const string visualStudioProgId = "VisualStudio.DTE";
            // Visual Studio DTE Object. Throws if Visual Studio is closed.
            dynamic dte = Marshal.GetActiveObject(visualStudioProgId);
            try
            {
                foreach (var dteWindow in dte.Windows)
                {
                    if (!dteWindow.Visible || dteWindow.Top == 0 && dteWindow.Left == 0)
                    {
                        // invisible windows and such that aren't on top of the z order
                        continue;
                    }

                    var windowRect = new Rectangle(dteWindow.Left, dteWindow.Top, dteWindow.Width, dteWindow.Height);
                    if (windowRect.Contains(point))
                    {
                        return AnonymizeWindowCaption(dteWindow.Caption);
                    }
                }
                return "Microsoft Visual Studio";
            }
            catch (Exception)
            {
                // ComException
                // ObjectDisposedException
                // ignore
                return string.Empty;
            }
            finally
            {
                Marshal.ReleaseComObject(dte);
            }
        }

        private static string AnonymizeWindowCaption(string windowCaption)
        {
            if (windowCaption.EndsWith(".cs"))
            {
                return "Code Window";
            }
            return windowCaption;
        }

    }
}
