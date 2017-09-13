// Created by André Meyer at MSR
// Created: 2015-12-02
// 
// Licensed under the MIT License.

using Shared.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;

namespace Shared.Data.Extractors
{
    public class Artifact
    {
        public string FileName { get; set; }
        public double DurationInMins { get; set; }
        /// <summary>
        /// not yet used
        /// </summary>
        public string FilePath { get; set; }
    }

    /// <summary>
    /// This class offers some methods to receive detailed information about an 
    /// artifact, extracted from the programs, tracked by the WindowsActivityTracker
    /// 
    /// Hint: The WindowsActivityTracker must be enabled to make use of the extractor
    /// </summary>
    public static class WindowTitleArtifactExtractor
    {
        #region Query Database for Window Titles

        public static List<Artifact> GetFilesWorkedOn(DateTimeOffset date)
        {
            var files = new List<Artifact>();

            try
            {
                var onlySearchForProgramsWhereRulesExists = GetSqlForProgramsToSearch();

                var query = "SELECT process, window, sum(difference) / 60.0 as 'durInMin' "
                          + "FROM ( "
                          + "SELECT process, window, (strftime('%s', tsEnd) - strftime('%s', tsStart)) as 'difference' "
                          + "FROM " + Shared.Settings.WindowsActivityTable + " "
                          + "WHERE " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, "tsStart") + " and " + Database.GetInstance().GetDateFilteringStringForQuery(VisType.Day, date, "tsEnd") + " "
                          + onlySearchForProgramsWhereRulesExists
                          + "GROUP BY id, tsStart "
                          + ") "
                          + "WHERE difference > 0 "
                          + "GROUP BY window "
                          + "ORDER BY durInMin DESC;";

                var table = Database.GetInstance().ExecuteReadQuery(query);

                foreach (DataRow row in table.Rows)
                {
                    var process = (string)row["process"];
                    var windowTitle = (string)row["window"];
                    var filename = GetArtifactDetails(process, windowTitle);
                    var durInMin = (double)row["durInMin"];

                    if (string.IsNullOrEmpty(filename) || durInMin < 1) continue;

                    var art = new Artifact
                    {
                        FileName = filename,
                        DurationInMins = durInMin
                    };

                    files.Add(art);
                }
                table.Dispose();
            }
            catch (Exception e)
            {
                Shared.Logger.WriteToLogFile(e);
            }

            return files;
        }

        private static string GetSqlForProgramsToSearch()
        {
            var artifactRules = BaseRules.ArtifactRules;
            var onlySearchForProgramsWhereRulesExists = string.Empty;
            if (artifactRules.Count > 0)
            {
                onlySearchForProgramsWhereRulesExists += "AND (";
                for (var i = 0; i < artifactRules.Count; i++)
                {
                    onlySearchForProgramsWhereRulesExists += "lower(process) = '" + artifactRules[i].ProcessName + "' ";
                    if (i + 1 < artifactRules.Count) onlySearchForProgramsWhereRulesExists += "or ";
                }
                onlySearchForProgramsWhereRulesExists += ") ";
            }

            return onlySearchForProgramsWhereRulesExists;
        }

        #endregion

        /// <summary>
        /// To get the file name and extension, we do the following;
        /// 1. remove unnecessary stuff like [Administrator] from the window title
        /// 2. remove program name from window title
        /// 3. get the file extension based on the window name and program
        /// </summary>
        /// <param name="process"></param>
        /// <param name="windowTitle"></param>
        /// <returns>File with an extension</returns>
        public static string GetArtifactDetails(string process, string windowTitle)
        {
            //process = BaseRules.RunApplicationHostTitleCleaning(process, windowTitle);
            var fileName = CleanWindowTitle(process, windowTitle);
            if (fileName.Length == 0) return string.Empty; // could not get filename
            var fileNameWithExtension = ValidateOrAddExtension(fileName, process);

            return fileNameWithExtension;
        }

        public static string CleanWindowTitle(string process, string title)
        {
            // run basis cleaning
            title = BaseRules.RunBasisArtifactTitleCleaning(title);

            var removables = BaseRules.GetRemovablesFromProcess(process, BaseRules.ArtifactRules);
            // remove programname from title 
            if (removables != null && removables.Count > 0)
            {
                foreach (var r in removables)
                {
                    title = Regex.Replace(title, r, "").Trim();
                }
            }

            // remove path
            if (title.Contains("/") || title.Contains("\\"))
            {
                title = GetFileNameFromPath(title);
            }

            return title.Trim();
        }

        /// <summary>
        /// Add a file extension if necessary
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="process"></param>
        /// <returns></returns>
        private static string ValidateOrAddExtension(string fileName, string process)
        {
            var extension = GetFileExtensionFromFileName(fileName);
            if (! string.IsNullOrEmpty(extension)) return fileName;

            var extensions = GetFileExtensionsFromProcess(process);
            if (extensions == null || extensions.Count == 0) return fileName;

            return fileName + extensions[0]; // just add the first one (not very nice yet)
        }

        /// <summary>
        /// Returns the file extension from file name
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetFileExtensionFromFileName(string fileName)
        {
            var ext = string.Empty;
            try
            {
                ext = Path.GetExtension(fileName);
            }
            catch { }
            return ext;
        }

        /// <summary>
        /// Returns the file name from a path
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetFileNameFromPath(string path)
        {
            var ext = string.Empty;
            try
            {
                ext = Path.GetFileName(path);
            }
            catch { }
            return ext;
        }

        /// <summary>
        /// Returns the file name from a path
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetFileNameWithoutExtension(string path)
        {
            var ext = path;
            try
            {
                ext = Path.GetFileNameWithoutExtension(path);
            }
            catch { }
            return ext;
        }

        /// <summary>
        /// Get the file extension from the process name
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        private static List<string> GetFileExtensionsFromProcess(string process)
        {
            foreach (var rule in BaseRules.ArtifactRules)
            {
                if (rule.ProcessName == process.ToLower()) return rule.FileExtensions;
            }

            return new List<string>(); // empty list
        }
    }
}
