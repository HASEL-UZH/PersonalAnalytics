// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Shared.Data
{
    /// <summary>
    /// This helper class tries to automatically classify an activity
    /// to one of the predefined categories (based on the available information).
    /// 
    /// Hint: The WindowsActivityTracker must be enabled to make use of the mapper
    /// </summary>
    public static class ContextMapper
    {
        #region Context Mapping Logic

        /// <summary>
        /// TODO: update comments 
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public static ContextCategory GetContextCategory(ContextDto dto)
        {
            try
            {
                var processName = dto.Context.ProgramInUse;
                var windowName = dto.Context.WindowTitle;

                if (string.IsNullOrEmpty(processName) && string.IsNullOrEmpty(windowName))
                    return ContextCategory.Unknown;

                if (windowName != null) windowName = windowName.ToLower();
                if (processName != null) processName = processName.ToLower();


                // all IDLE, will later manually check with more info to find meetings, breaks, etc.
                if (processName != null && processName.Equals(Dict.Idle.ToLower()))
                    return ContextCategory.None; //TODO: add logic for <2min idle (reading something)

                // check with planning keywords
                if (IsCategory(ContextCategory.Planning, processName, windowName))
                {
                    // this is needed because "task" could be mapped for the "task manager"
                    return IsCategory(ContextCategory.Other, processName, windowName)
                        ? ContextCategory.Other
                        : ContextCategory.Planning;
                }
                // if not planning, check with email keywords
                if (IsCategory(ContextCategory.Email, processName, windowName))
                {
                    return ContextCategory.Email;
                }
                // if editor, might be reading/writing OR coding (if common coding file type extension or the window
                // title has not enough information to accurately map by hand, then map to coding category,
                // else: manual mapping later)
                if (IsEditor(processName, windowName))
                {
                    return (IsCodeFile(windowName))
                        ? ContextCategory.DevCode
                        : ContextCategory.ReadWriteDocument; //ContextCategory.ManualEditor;
                }
                // check with debugging keywords (manual because of manual checking later)
                if (IsCategory(ContextCategory.DevDebug, processName, windowName))
                {
                    return ContextCategory.DevDebug;
                }
                // check with review keywords (manual because of manual checking later)
                if (IsCategory(ContextCategory.DevReview, processName, windowName))
                {
                    return ContextCategory.DevReview;
                }
                // check with version control keywords (manual because of manual checking later)
                if (IsCategory(ContextCategory.DevVc, processName, windowName))
                {
                    return ContextCategory.DevVc;
                }
                // check with coding keywords (there might be more from editors, manual mapping)
                if (IsCategory(ContextCategory.DevCode, processName, windowName))
                {
                    return ContextCategory.DevCode;
                }
                // check with read/write keywords (there might be more from editors, manual mapping)
                if (IsCategory(ContextCategory.ReadWriteDocument, processName, windowName))
                {
                    return ContextCategory.ReadWriteDocument;
                }

                // NO automated mapping of formal meetings: will look at self-reported tasks & IDLE times
                // NO automated mapping of in-formal meetings: will look at self-reported tasks & IDLE times

                
                // check if its a browser (did not yet fit into other categories
                // then it's work related / unrelated web browsing which is manually mapped
                if (IsBrowser(processName))
                {
                    // map according to keywords and websites
                    if (IsWebsiteWorkRelated(windowName))
                        return ContextCategory.WorkRelatedBrowsing;
                    if (IsWebsiteWorkUnrelated(windowName))
                        return ContextCategory.WorkUnrelatedBrowsing;

                    return ContextCategory.WorkRelatedBrowsing; // default
                }
                // check with instant messaging keywords (subcategory of INFORMAL MEETING)
                if (IsCategory(ContextCategory.InformalMeeting, processName, windowName))
                {
                    return ContextCategory.InformalMeeting;
                }
                // check with rdp keywords (subcategory of Other)
                if (IsCategory(ContextCategory.OtherRdp, processName, windowName))
                {
                    return ContextCategory.OtherRdp;
                }
                // check if it's something else (OS related, navigating, etc.)
                if (IsCategory(ContextCategory.Other, processName, windowName))
                {
                    return ContextCategory.Other;
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return ContextCategory.Unknown; // should never happen!
        }

        private static bool IsCategory(ContextCategory category, string processName, string windowName)
        {
            var listToCheck = GetListForCategory(category);
            if (listToCheck == null) return false;
            return listToCheck.Any(processName.Contains) || listToCheck.Any(windowName.Contains);
        }

        private static List<string> GetListForCategory(ContextCategory cat)
        {
            switch (cat)
            {
                case ContextCategory.DevCode:
                    return CodingApps;
                case ContextCategory.DevDebug:
                    return CodingDebugApps;
                case ContextCategory.DevReview:
                    return CodingReviewApps;
                case ContextCategory.DevVc:
                    return CodingVersionControlApps;
                case ContextCategory.Email:
                    return EmailApps;
                case ContextCategory.Planning:
                    return PlanningApps;
                case ContextCategory.ReadWriteDocument:
                    return ReadingWritingApps;
                case ContextCategory.InformalMeeting:
                    return InstantMessagingApps;
                case ContextCategory.Other:
                    return OtherApps;
                case ContextCategory.OtherRdp:
                    return OtherRdpApps;
            }
            return null;
        }

        private static bool IsEditor(string processName, string windowName)
        {
            return EditorApps.Any(processName.Contains) || EditorApps.Any(windowName.Contains);
        }

        private static bool IsBrowser(string processName)
        {
            return BrowserApps.Any(processName.Contains);
        }

        private static bool IsWebsiteWorkRelated(string windowName)
        {
            return WorkRelatedBrowsingWebsites.Any(windowName.Contains);
        }

        private static bool IsWebsiteWorkUnrelated(string windowName)
        {
            return WorkUnrelatedBrowsingKeywords.Any(windowName.Contains);
        }

        private static bool IsCodeFile(string windowName)
        {
            return CodeTypeApps.Any(windowName.Contains);
        }

        #endregion

        #region Context Mapping Lists

        private static readonly List<string> EditorApps = new List<string> { "notepad", "xmlspy", "sublime", "emacs", "vim", "atom", "texteditor", "editplus", "gedit", "textpad" };
        private static readonly List<string> EditorNotEnoughInfoList = new List<string> { "go to...", "find", "untitled - notepad", "save", "speichern unter", "speichern", "suche", "replace", "ersetzen", "*new", "open", "reload", "new", "save as" }; // from notepad++
        private static readonly List<string> CodingApps = new List<string> { "webplatforminstaller", "xts", "cleardescribe", "clearfindco", "alm-client", "ildasm", "ssms", "mintty", "xming", "clearprojexp", "clearmrgman", "kitty", "bc2", "bcompare", "mobaxterm", "webmatrix", "mexplore", "linqpad", "android sdk manager", "windows phone application deployment", "ilspy", "tortoiseproc", "xsd", "eclipse", "fiddler", "xamarin", "netbeans", "intellij", "sql", "sqlitebrowser", "devenv", "visual studio", "vs_enterprise", "vs2013", "microsoftazuretools", "webstorm", "phpstorm", "source insight", "zend", "console", "powershell", "shell", "cmd", "tasktop", "android studio", "ide", "filezilla", "flashfxp", "charles" };
        private static readonly List<string> CodeTypeApps = new List<string> { ".proj", ".cmd", ".ini", ".err", ".sql", ".ksh", ".dat", ".xaml", ".rb", ".kml", ".log", ".bat", ".cs", ".vb", ".py", ".xml", ".dtd", ".xs", ".h", ".cpp", ".java", ".class", ".js", ".asp", ".aspx", ".css", ".html", ".htm", ".js", ".php", ".xhtml", ".sh", ".sln", ".vcxproj", ".pl" };
        private static readonly List<string> CodingDebugApps = new List<string> { "xde", "debug" }; // works for visual studio, eclipse (if view changes), NOT for intellij IDEA
        private static readonly List<string> CodingReviewApps = new List<string> { "codeflow", "gerrit", "stash", "kallithea", "code review", "rhodecode", "rietveld", "crucible", "phabricator" };
        private static readonly List<string> CodingVersionControlApps = new List<string> { "repository", "cleardiffbl", "cleardlg", "cleardiffmrg", "clearhistory", "clearvtree", "sourcetree", "svn", "tortoiseproc", "scm", "tfs", "push", "pull", "commit", "gitlab", "github", "bitbucket", "visual studio online" };

        private static readonly List<string> EmailApps = new List<string> { "mail", "outlook", "thunderbird", "outlook.com" }; // incudes gmail, yahoo mail, mac mail, outlook.com //TODO: special case: outlook
        private static readonly List<string> PlanningApps = new List<string> { "backlog", "winproj", "sap", "rescuetime", "clearquest", "scrum", "kanban", "codealike", "jira", "rally", "versionone", "calendar", "kalender", "sprint", "user story", "plan", "task", "aufgabe", "vorgangsliste", "work item" };
        private static readonly List<string> ReadingWritingApps = new List<string> { "snagiteditor", "confluence", "picasa", "windows photo viewer", "flashmedialiveencoder", "photofiltre", "jmp", "treepad", "winword", "word", "leo", "translate", "übersetzer", "mspub", "excel", "powerpnt", "onenote", "evernote", "acrord", "sharepoint", "pdf", "foxitreader", "adobe reader", "reader", "glcnd", "wiki", "keep", "google docs", "yammer", "docs", "office", "paint", "gimp", "photoshop", "lightroom", "tex", "latex", "photo", "foto" }; //not "note" as notepad is more coding
        private static readonly List<string> InstantMessagingApps = new List<string> { "skype", "lync", "sip", "g2mlauncher", "ciscowebexstart", "nbrplay", "g2mui", "chatter", "atmgr", "hangout", "viber" }; // includes skype for business

        private static readonly List<string> BrowserApps = new List<string> { "iexplore", "chrome", "firefox", "opera", "safari", "applicationframehost", "edge" }; // ApplicationFrameHost stands for Microsoft Edge
        private static readonly List<string> WorkUnrelatedBrowsingKeywords = new List<string> { "gopro", "saldo", "halo", "book", "party", "swag", "birthday", "therapy", "vacation", "wohnung", "flat", "airbnb", "money", "hotel", "mietwagen", "rental", "credit", "hockeybuzz.com", "empatica", "wallpaper", "flight", "travel", "store", "phone", "buy", "engadget", "motorcycle", "car", "auto", "honda", "bmw", "nissan", "subaru", "winter", "summer", "bike", "bicycle", "arcgis", "finance", "portfolio", "toy", "gadget", "geek", "wellness", "health", "saturday", "sunday", "weekend", "sushi", "eat", "dessert", "restaurant", "holiday", "hotel", "cafe", "gas", "deal", "shop", "shopping", "craigslist", "vancouver", "indoor", "club", "loan", "maps", "flower", "florist", "valentine", "zalando", "tripadvisor", "golem", "pr0gramm", "tilllate", "heise", "jedipedia", "blick", "daydeal.ch", "renovero", "brack.ch", "skyscanner", "easyjet", "booking.com", "meteocheck", "scientific american", "ars technica", "national post", "sensecore", "core pro", "| time", "hockey inside/out", "netflix", "wired", "popular science", "habsrus", "flickr", "imdb", "xkcd", "derStandard.at", "amazon", "nhl.com", "20 minuten", "facebook", "reddit", "twitter", "google+", "news", "aktuell", "9gag", "youtube", "vimeo", "yahoo", "comic", "ebay", "ricardo", "whatsapp", "stream", "movie", "cinema", "kino", "music", "musik", "tumblr" };
        private static readonly List<string> WorkRelatedBrowsingWebsites = new List<string> { "batmon", "calculator", "analytics", "azure", "power bi", "business", "php", "proffix", "centmin", "picturex", "ios", "schmelzmetall", "natur- und tierpark goldau", "tierpark", "amazon web service", "cyon", "salesforce.com", "silverlight", "issue", "junit", "mylyn", "jetbrains", "telerik", "testcomplete", "application lifecycle management", "all reports", "advanced search", ".net", "c#", "java", "vbforums", "dashboard", "virtualbox", "document", "dropbox", "onedrive", "proxy", "jenkins", "databasics", "suite", "abb", "shadowbot", "office", "windows", "namespace", "ventyx", "api", "apache", "oracle", "server", "system", "ibm", "code", "codeplex", "retrospection", "stack overflow", "msdn", "developer", "documentation", "blog", "coding", "programmer" };

        private static readonly List<string> OtherRdpApps = new List<string> { "mstsc", "vmware", "vpxclient", "msiexec", "pageant", "putty" };
        private static readonly List<string> OtherApps = new List<string> { "zune", "itunes", "vlc", "music", "groove", "musik", "spotify", "wmplayer", "mmc", "vpnui", "dinotify", "perfmon", "agentransack", "lockapp", "searchui", "pwsafe", "personalanalytics", "wuauclt", "calc", "zip", "googleearth", "rar", "wwahost", "update", "avpui", "procexp64", "taskmgr", "pgp", "explorer", "groove", "dwm", "rstrui", "snippingtool", "onedrive", "settings", "einstellungen" };
        // no categories for managers (e.g. SAP, Axapta, MS Project --> currently mapped to planning)
        // no list for planned meetings (fetch from calendar)
        
        #endregion

    }
}
