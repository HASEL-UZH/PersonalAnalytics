using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Shared.Data.Extractors;

namespace TimeSpentVisualizer
{
    public class ArtifactTest
    {
        public ArtifactTest(string inputTitle, string inputProcess, string outputExpected)
        {
            InputTitle = inputTitle;
            InputProcess = inputProcess;
            OutputExpected = outputExpected;
        }
        public string InputTitle { get; set; }
        public string InputProcess { get; set; }
        public string OutputExpected { get; set; }
    }

    [TestClass]
    public class WindowTitleArtifactExtractorTest
    {
        [TestMethod]
        public void GetArtifactDetailsTest()
        {
            // dummy data
            var dummyList = new List<ArtifactTest>();
            dummyList.Add(new ArtifactTest("Document.docx - Word", "WINWORD", "Document.docx"));
            dummyList.Add(new ArtifactTest("Lost Receipts Form V1.doc [Read-Only] [Compatibility Mode] - Word", "WINWORD", "Lost Receipts Form V1.doc"));
            dummyList.Add(new ArtifactTest("Book1 - Excel", "excel", "Book1.xlsx"));
            dummyList.Add(new ArtifactTest("PBI-WSR  [Read-Only] - Excel", "excel", "PBI-WSR.xlsx"));
            dummyList.Add(new ArtifactTest("20150305 - Analytics Experience Review - PowerPoint", "powerpnt", "20150305 - Analytics Experience Review.pptx"));
            dummyList.Add(new ArtifactTest("Publication2 (Read-Only) - Publisher", "mspub", "Publication2.pub"));
            dummyList.Add(new ArtifactTest("Acquisition - OneNote", "onenote", "Acquisition.one"));
            dummyList.Add(new ArtifactTest("B18 TSE Nameplate  [Read-Only] - Visio Professional", "visio", "B18 TSE Nameplate.vsd"));
            dummyList.Add(new ArtifactTest(".gitignore - Notepad", "notepad", ".gitignore"));
            dummyList.Add(new ArtifactTest("Find ", "notepad", ""));
            dummyList.Add(new ArtifactTest("hosts - Notepad", "notepad", "hosts.txt"));
            dummyList.Add(new ArtifactTest("20130617 - Bing Scorecard DAX Queries - Notepad", "notepad", "20130617 - Bing Scorecard DAX Queries.txt"));
            dummyList.Add(new ArtifactTest(@"*C:\DATA\MS-PA\AM.PA.MonitoringTool\Retrospection\Properties\styles_css.txt", "notepad", "styles_css.txt"));
            dummyList.Add(new ArtifactTest(@"c:\EngDev\config\batmon\Q-Prod-Co3\QSignTest\QSignSettings.json - Notepad++ [Administrator]", "notepad++", "QSignSettings.json"));
            dummyList.Add(new ArtifactTest(@"C:\Users\kimh\Downloads\QTestResults.wtt.xml (r_scripts) - Sublime Text 2", "sublime", "QTestResults.wtt.xml"));
            dummyList.Add(new ArtifactTest(@"C:\Users\kimh\AppData\Roaming\Sublime Text 2\Packages\User\Default (Windows).sublime-keymap • (r_scripts) - Sublime Text 2", "sublime", "Default (Windows).sublime-keymap"));
            dummyList.Add(new ArtifactTest("Print ", "foxitreader", ""));
            dummyList.Add(new ArtifactTest("PassportApplicationComplete (1).pdf - Foxit Reader", "foxitreader", "PassportApplicationComplete (1).pdf"));
            dummyList.Add(new ArtifactTest("Nonimmigrant Visa - Confirmation Page.pdf - Adobe Reader", "AcroRd32", "Nonimmigrant Visa - Confirmation Page.pdf"));
            dummyList.Add(new ArtifactTest("Nonimmigrant Visa - Confirmation Page.pdf - Adobe Acrobat Reader DC", "AcroRd32", "Nonimmigrant Visa - Confirmation Page.pdf"));
            dummyList.Add(new ArtifactTest("2015-11-29 MS Covergence 15 Bar 24 by Picturex (1320284).jpg ?- Photos", "applicationframehost", "2015-11-29 MS Covergence 15 Bar 24 by Picturex (1320284).jpg"));

            // run method
            foreach (var row in dummyList)
            {
                var fileName = WindowTitleArtifactExtractor.GetArtifactDetails(row.InputProcess, row.InputTitle);

                Debug.WriteLine(row.OutputExpected + " - vs - " + fileName);
                Assert.AreEqual(row.OutputExpected, fileName);
            }
        }

        [TestMethod]
        public void GetWebsiteDetailsTest()
        {
            // dummy data
            var dummyList = new List<ArtifactTest>();
            dummyList.Add(new ArtifactTest("Your retrospection for the 7/14/2015 - Microsoft Edge", "ApplicationFrameHost", "Your retrospection for the 7/14/2015"));
            dummyList.Add(new ArtifactTest("Your retrospection for the 7/14/2015 ?- Microsoft Edge", "MicrosoftEdge", "Your retrospection for the 7/14/2015"));
            dummyList.Add(new ArtifactTest("SiriusXM - Channel Line up - Google Chrome", "chrome", "SiriusXM - Channel Line up"));
            dummyList.Add(new ArtifactTest("Grant access to Visual Studio Online for Power BI - Internet Explorer", "iexplore", "Grant access to Visual Studio Online for Power BI"));
            dummyList.Add(new ArtifactTest("Search | King County Library System | BiblioCommons - Mozilla Firefox", "firefox", "Search | King County Library System | BiblioCommons"));
            dummyList.Add(new ArtifactTest("ProductsWeb - Internet Explorer", "iexplore", "ProductsWeb"));
            dummyList.Add(new ArtifactTest("(1) WhatsApp Web - Internet Explorer", "iexplore", "WhatsApp Web"));
            dummyList.Add(new ArtifactTest("(2) WhatsApp Web ?- Microsoft Edge", "microsoftedge", "WhatsApp Web"));
            dummyList.Add(new ArtifactTest("(3) WhatsApp Web - Microsoft Edge", "microsoftedge", "WhatsApp Web"));
            dummyList.Add(new ArtifactTest("(4) WhatsApp Web - Google Chrome", "chrome", "WhatsApp Web"));
            dummyList.Add(new ArtifactTest("Testing.pptx [Geschuetzte Ansicht] - Mozilla Firefox", "firefox", "Testing.pptx"));
            dummyList.Add(new ArtifactTest("Testing.pptx [Geschuetzte Ansicht] (4) - Mozilla Firefox", "firefox", "Testing.pptx"));
            dummyList.Add(new ArtifactTest("Testing.pptx (4) (4) [Geschuetzte Ansicht] - Mozilla Firefox", "firefox", "Testing.pptx"));

            //dummyList.Add(new ArtifactTest("chrome", "Save as", ""));

            // run method
            foreach (var row in dummyList)
            {
                var website = WindowTitleWebsitesExtractor.GetWebsiteDetails(row.InputProcess, row.InputTitle);

                Debug.WriteLine(row.OutputExpected + " - vs - " + website);
                Assert.AreEqual(row.OutputExpected, website);
            }
        }

        [TestMethod]
        public void GetVisualStudioProjectsTest()
        {
            // dummy data
            var dummyList = new List<ArtifactTest>();
            dummyList.Add(new ArtifactTest("ConsoleApplication1 (Debugging) - Microsoft Visual Studio (Administrator)", "", "ConsoleApplication1"));
            dummyList.Add(new ArtifactTest("ConsoleApplication1 (Running) - Microsoft Visual Studio (Administrator)", "", "ConsoleApplication1"));
            dummyList.Add(new ArtifactTest("ConsoleApplication1 - Microsoft Visual Studio", "", "ConsoleApplication1"));
            dummyList.Add(new ArtifactTest("Add New Item - SchemaGenerator", "", ""));
            dummyList.Add(new ArtifactTest("Add New Item - Query Service Model", "", ""));
            dummyList.Add(new ArtifactTest("sources_dev_transportsync_src_common.dgml - Microsoft Visual Studio", "", "sources_dev_transportsync_src_common.dgml"));
            dummyList.Add(new ArtifactTest("Microsoft Visual Studio", "", ""));

            // run method
            foreach (var row in dummyList)
            {
                var title = WindowTitleCodeExtractor.GetProjectName(row.InputTitle);

                Debug.WriteLine(row.OutputExpected + " - vs - " + title);
                Assert.AreEqual(row.OutputExpected, title);
            }
        }

        [TestMethod]
        public void GetCodeReviewsDoneTest()
        {
            // CodeFLow Code Reviews

            // dummy data
            var dummyList = new List<ArtifactTest>();
            dummyList.Add(new ArtifactTest("Set all Year columns to Year category - CodeFlow", "", "Set all Year columns to Year category"));
            dummyList.Add(new ArtifactTest("Synchronizing Review... - CodeFlow", "", ""));
            dummyList.Add(new ArtifactTest("Compensating table query - CodeFlow", "", "Compensating table query"));

            // run method
            foreach (var row in dummyList)
            {
                var review = WindowTitleCodeExtractor.GetReviewName(row.InputTitle);

                Debug.WriteLine(row.OutputExpected + " - vs - " + review);
                Assert.AreEqual(row.OutputExpected, review);
            }

            ///////////////////////////////////////////////////////////
            // VS Code Reviews
            var dummyList2 = new List<ArtifactTest>();
            dummyList2.Add(new ArtifactTest("Code Review - Site.css", "", "Site.css"));
            dummyList2.Add(new ArtifactTest("AM.PA.MonitoringTool(Running) - Microsoft Visual Studio", "", ""));

            foreach (var row in dummyList2)
            {
                var review = WindowTitleCodeExtractor.GetVsReviewName(row.InputTitle);

                Debug.WriteLine(row.OutputExpected + " - vs - " + review);
                Assert.AreEqual(row.OutputExpected, review);
            }
        }

        [TestMethod]
        public void GetEmailDetailsTest()
        {
            // dummy data
            var dummyList = new List<ArtifactTest>();
            dummyList.Add(new ArtifactTest("Untitled - Task", "", "Untitled - Task"));
            dummyList.Add(new ArtifactTest("To-Do List - t-anmeye@microsoft.com - Outlook", "", "To-Do List"));
            dummyList.Add(new ArtifactTest("Calendar - t-anmeye@microsoft.com - Outlook", "", "Calendar"));
            dummyList.Add(new ArtifactTest("_Talks - t-anmeye@microsoft.com - Outlook", "", "_Talks"));
            dummyList.Add(new ArtifactTest("Give me all the Pictures... - Message (HTML)", "", "Give me all the Pictures... - Message (HTML)"));

            // run method
            foreach (var row in dummyList)
            {
                var review = WindowTitleEmailExtractor.CleanWindowTitle(row.InputTitle);

                Debug.WriteLine(row.OutputExpected + " - vs - " + review);
                Assert.AreEqual(row.OutputExpected, review);
            }
        }
    }
}
