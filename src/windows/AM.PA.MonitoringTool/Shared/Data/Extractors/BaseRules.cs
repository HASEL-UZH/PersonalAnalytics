
// Created by André Meyer at MSR
// Created: 2015-12-03
// 
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Shared.Data.Extractors
{
    public static class BaseRules
    {
        /// <summary>
        /// remove [Read-Only][Administrator][Compatibly Mode]
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public static string RunBasisArtifactTitleCleaning(string title)
        {
            var cleanList = new List<string> { "[Read-Only]", "(Read-Only)", "[Administrator]", "[Compatibility Mode]", "(Protected View)", "*", "Print ", "Save as ", "save as ", "Find", "find", "Go to...", "go to..." };
            //var removeEditorActions = new List<string> { "go to...", "find", "untitled", "save", "speichern unter", "speichern", "suche", "replace", "ersetzen", "*new", "open", "reload", "new", "save as" }; // from notepad++

            foreach (var c in cleanList)
            {
                title = title.Replace(c, "");
            }

            return title;
        }

        private static List<string> _possibleEditorExtensions = new List<string> { ".txt", ".rtf", ".tex", ".tmp", ".bak", ".csv", ".dat", ".gitignore", ".sln", ".proj", ".vcxproj", ".xcodeproj", ".cmd", ".ini", ".err", ".sql", ".ksh", ".dat", ".xaml", ".rb", ".kml", ".log", ".bat", ".cs", ".swift", ".vb", ".py", ".xml", ".dtd", ".xs", ".h", ".cpp", ".java", ".class", ".js", ".asp", ".aspx", ".css", ".html", ".htm", ".js", ".php", ".xhtml", ".sh", ".pl" };

        /// <summary>
        /// basic rules for Artifact Extractor
        /// </summary>
        public static List<ProgramInfo> ArtifactRules = new List<ProgramInfo>
        {
            // Office
            new ProgramInfo("winword", new List<string>{ ".docx", ".doc"}, @"\- Word"),
            new ProgramInfo("excel", new List<string>{ ".xlsx", ".xls"}, @"\- Excel"),
            new ProgramInfo("powerpnt",  new List<string>{ ".pptx", ".ppt"}, @"\- PowerPoint"),
            new ProgramInfo("onenote", ".one", @"\- OneNote"),
            new ProgramInfo("mspub", ".pub", @"\- Publisher"),
            new ProgramInfo("visio", ".vsd", @"\- Visio Professional"),

            // PDF Readers
            new ProgramInfo("acrord32", ".pdf", @"\- Adobe(.*)Reader(.*)$"), // also removes Adobe Acrobat Reader DC
            new ProgramInfo("acrobat", ".pdf", @"\- Adobe Acrobat Pro(.*)$"), // also removes Adobe Acrobat Pro DC
            new ProgramInfo("foxitreader", ".pdf", @"\- Foxit Reader"),

            // Editors
            new ProgramInfo("notepad", _possibleEditorExtensions, @"\- Notepad"),
            new ProgramInfo("notepad2", _possibleEditorExtensions, @"\- Notepad2"),
            new ProgramInfo("notepad++", _possibleEditorExtensions, @"\- Notepad\+\+"),
            new ProgramInfo("sublime", _possibleEditorExtensions, new List<string> { @"\- Sublime Text(.*)$", @"\(r_scripts\)", "•" }),

            // SQL (MySQLWorkbench doesn't have window titles)
            new ProgramInfo("sqlitebrowser", ".dat", @"DB Browser for SQLite \-"),

            // Photo programs
            new ProgramInfo("photos", @"[\?]?\- Photos"),

            // Latex programs
            new ProgramInfo("texstudio", ".tex", @"\- TeXstudio"),
            new ProgramInfo("texmaker", ".tex", @"Document : "),
        };


        static string removeStuffInBrackets1 = @"(\[([^\]]*)\])+";
        static string removeStuffInBrackets2 = @"(\(([^\)]*)\))+";

        /// <summary>
        /// basic rules for Website Extractor
        /// </summary>
        public static List<ProgramInfo> WebsiteRules = new List<ProgramInfo>
        {
            new ProgramInfo("iexplore", new List<string> { @"\- Internet Explorer", removeStuffInBrackets1, removeStuffInBrackets2 }),
            new ProgramInfo("microsoft edge", new List<string> { @"[\?]?\- Microsoft Edge", removeStuffInBrackets1, removeStuffInBrackets2 }),
            new ProgramInfo("microsoftedge", new List<string> { @"[\?]?\- Microsoft Edge", removeStuffInBrackets1, removeStuffInBrackets2 }),
            //new ProgramInfo("applicationframehost", new List<string> { @"[\?]?\- Microsoft Edge", removeStuffInBrackets1, removeStuffInBrackets2 }),  // TODO: remove at some point, as WindowsActivityTracker handles it
            new ProgramInfo("firefox",  new List<string> { @"\- Mozilla Firefox", removeStuffInBrackets1, removeStuffInBrackets2 }),
            new ProgramInfo("chrome", new List<string> { @"\- Google Chrome", removeStuffInBrackets1, removeStuffInBrackets2 }),
            new ProgramInfo("opera", new List<string> { @"\- Opera", removeStuffInBrackets1, removeStuffInBrackets2 }),
        };

        /// <summary>
        /// basic rules for People Extractor
        /// </summary>
        public static List<string> PeopleRules = new List<string>
        {
           "skype for business", "conversation", "participants", "dial-in"
        };

        /// <summary>
        /// basic rules for Outlook Extractor (WindowTitleEmailExtracotr)
        /// </summary>
        public static List<string> OutlookRules = new List<string>
        {
            @" - .*@.* - Outlook" //todo: add more rules
        };

        /// <summary>
        /// basic rules for Visual Studio Extractor
        /// </summary>
        public static List<string> VisualStudioRules = new List<string>
        {
           @"(\-)?\s?Microsoft Visual Studio", @"\(Administrator\)", @"\(Debugging\)", @"\(Running\)", @"\[Read Only\]", @"\*", "Extensions and Updates", "Add New Item - .*$"
        };

        /// <summary>
        /// basic rules for Code Reviews Extractor
        /// </summary>
        public static List<string> CodeReviewRules = new List<string>
        {
            "- CodeFlow", "Synchronizing Review..."
        };

        /// <summary>
        /// basic rules for Visual Studio Code Rreviews Extractor
        /// </summary>
        public static List<string> VisualStudioCodeReviewRules = new List<string>
        {
            "Code Review -"
        };


        #region Helpers

        /// <summary>
        /// Returns a list of removables for a given process
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        internal static List<string> GetRemovablesFromProcess(string process, List<ProgramInfo> rules)
        {
            foreach (var rule in rules)
            {
                if (rule.ProcessName == process.ToLower()) return rule.RemovablesRegex;
            }

            return new List<string>(); // empty list
        }

        #endregion
    }
}
