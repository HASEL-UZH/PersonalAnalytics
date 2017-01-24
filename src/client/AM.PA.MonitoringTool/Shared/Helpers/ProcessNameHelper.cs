
using System.Globalization;

namespace Shared.Helpers
{
    public static class ProcessNameHelper
    {
        /// <summary>
        /// Gets the file description of a proces and returns it formatted
        /// (shortened if neccessary)
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public static string GetFileDescription(string process)
        {
            var fileDesc = GetFileDescriptionFromProcess(process);

            // to title case
            var textInfo = new CultureInfo("en-US", false).TextInfo;
            fileDesc = textInfo.ToTitleCase(fileDesc.ToLower());

            // shorten file description if necessary
            if (fileDesc == null)
            {
                fileDesc = process;
                if (fileDesc.Length > 20)
                    fileDesc = "..." + fileDesc.Substring(fileDesc.Length - 17);
            }
            else if (fileDesc.Length > 20)
            {
                fileDesc = fileDesc.Substring(0, 17) + "...";
            }

            return fileDesc;
        }

        /// <summary>
        /// gets the name of the process
        /// TODO: currently doesn't work
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public static string GetFileDescriptionFromProcess(string process)
        {
            process = process.ToLower();

            // some nice program name hacks
            if (process == "applicationframehost") return "Microsoft Edge"; // TODO: remove at some point (should be fixed now)
            else if (process == "devenv") return "Visual Studio";
            else if (process == "winword") return "Word";
            else if (process == "powerpnt") return "Power Point";
            else if (process == "onenote") return "One Note";
            else if (process == "iexplore") return "Internet Explorer";
            else if (process == "acrord32") return "Adobe Reader";
            else if (process == "acrobat") return "Adobe Acrobat";
            else if (process == "ps") return "Product Studio";
            else if (process == "lync") return "Skype for Business";
            else if (process == "mstsc") return "Remote Desktop";
            else if (process == "taskmgr") return "Task Manager";
            else if (process == "ssms") return "SQL Server Management Studio";
            else if (process == "mendeleydesktop") return "Mendeley";

            return process;
            // todo: create or use program mapper

            //try
            //{
            //    var versionInfo = FileVersionInfo.GetVersionInfo(process);
            //    var res = (versionInfo.FileDescription == string.Empty) ? null : versionInfo.FileDescription;
            //    return res;
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e.Message);
            //    return null;
            //}
        }

        /// <summary>
        /// Fetches and calculates the total hours a developer worked on for a given date
        /// (based on the first non-idle entry).
        /// TODO: make more accurate by removing IDLE time
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        //public double GetTotalHoursWorked(DateTimeOffset date)
        //{
        //var totalHours = 0.0;
        //var firstEntryDateTime = DateTime.Now;
        //var lastEntryDateTime = DateTime.Now;

        //firstEntryDateTime = Database.GetInstance().GetUserWorkStart(date);
        //lastEntryDateTime = Database.GetInstance().GetUserWorkEnd(date);

        //totalHours = lastEntryDateTime.TimeOfDay.TotalHours - firstEntryDateTime.TimeOfDay.TotalHours;
        //return Math.Round(totalHours, 1);
        //}
    }
}
