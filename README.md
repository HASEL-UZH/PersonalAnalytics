# PersonalAnalytics - Building the Fitbit for Knowledge Workers
Personal Analytics project was originally initiated by [Dr. André N. Meyer](https://www.andre-meyer.ch) and [Prof. Dr. Thomas Fritz](http://www.ifi.uzh.ch/en/seal/people/fritz.html) from the SEAL Lab at the University of Zurich (UZH). Their goal is to build a self-monitoring tool that knowledge workers (e.g. developers, designers, administrators) install on their computer and that allows them to get insights into their work, work habits, and productivity, and identify positive behavior changes and opportunities for self-improvement. The basis are a number of computer interaction trackers (e.g. application usage, websites, emails/meetings, user input) and biometric trackers (e.g. Fitbit, Polar, Garmin, Tobii Eye-Tracker) that non-intrusively collect data, store them locally on the users machine (to avoid privacy issues!) and then visualize them in a daily/weekly summary, the retrospection. 

Anyone is welcome to contribute to PersonalAnalytics by extending it with new trackers or improving existing ones. Feel free to use PersonalAnalytics to get insights into your own work habits, or use it for research studies. In case you re-use PersonalAnalytics, make sure to cite our work (specifically this Github repository and our CSCW'2018 paper below).

## Releases
- [Windows (Installer)](https://www.andre-meyer.ch/DATA/PA/RELEASE/index.html) (note that Windows SmartScreen might ask you to unblock the install) 
- [Windows (Release)](https://github.com/HASEL-UZH/PersonalAnalytics/releases/tag/v0.9.6.3)
- [MacOS (Preview Release)](https://github.com/HASEL-UZH/PersonalAnalytics/releases/tag/macOS-v0.0.2.0)

## Further Information
- [Privacy information](https://github.com/HASE-UZH/PersonalAnalytics/blob/dev-am/documentation/PRIVACY.md)
- [Architecture information](https://github.com/HASE-UZH/PersonalAnalytics/blob/dev-am/documentation/ARCHITECTURE.md)
- Questions? contact [Dr. André Meyer](mailto:ameyer@ifi.uzh.ch)


![Retrospection Screenshot](./documentation/images/retrospection_screenshot.png?raw=true)

# Updates & Branches
- September 2014: Initiated by [André Meyer](https://www.andre-meyer.ch) and regularly updated since then.
- November, 2015: OpenSourced the project (license: MIT).
- February, 2016: Improved the retrospection and added the Office 365 tracker during an internship at Microsoft Research between November, 2015 and February, 2016
- April, 2016: Merged the branch (from Microsoft Research) with the original version, following the open sourcing of the code.
- May, 2016: Started working on a communication dashboard (including more insights into interactions with others) with ABB Research (on a separate branch). This work has never been finished.
- June, 2016: Started working on integrating the Muse tracker with [Monica Rüegg](https://github.com/montrin) (master student at the University of Zurich, Switzerland) on the 'muse' branch. The branch has not stable enough to merge with master.
- December, 2016: Started working on integrating several other biometric sensors (Polar, Garmin, Fitbit) (on the 'biometrics' branch). The PolarTracker and GarminTracker are available and stable on the master branch. GarminTracker is still in development.
- February, 2017: Integrated the (privately developed) [FlowLight](https://www.andre-meyer.ch/flowlight) to avoid interruptions at inopportune moments. It has since then been removed from the repository, as it was licensed to [Embrava](https://embrava.com/pages/flow).
- March, 2017: Started working on task type detection (on the 'taskdetection' branch), still in development.
- September, 2018: [Chris Satterfield](https://github.com/csatterfield) (master student at University of British Columbia, Canada) started integrating his port from Windows to MacOS. [mac-branch](https://github.com/HASE-UZH/PersonalAnalytics/tree/mac)
- September 2018: [Jan Pilzer](https://github.com/hirse) and [Raphael](https://github.com/raphaelro) started integrating their work on Tobii Eytracking. Work in progress.
- January 2019: [Louie Quaranta](https://github.com/louieQ) added an emotion state experience sampling pop-up to PersonalAnalytics for Mac (merged to [mac-branch](https://github.com/sealuzh/PersonalAnalytics/tree/mac)).
- October 2019: [Roy Rutishauser](https://github.com/royru) started unifying PersonalAnalytics for Mac and make it more similar to the Windows version
- May 2020: [Jan Pilzer](https://github.com/hirse) created a [large pull-request](https://github.com/sealuzh/PersonalAnalytics/pull/258) to include WindowDimmer into the official PersonalAnalytics release. [Publication](https://andre-meyer.ch/CHI20)
- October 2020: [Philip Hofmann](https://github.com/Phhofm) created a [large pull-request](https://github.com/HASE-UZH/PersonalAnalytics/pull/265) to include FocusSession into the official PersonalAnalytics release

# Main Contributors and People Involved
- [Dr. André Meyer](https://www.andre-meyer.ch) (University of Zurich, main contributor to Windows version)
- [Prof. Dr. Thomas Fritz](http://www.ifi.uzh.ch/en/seal/people/fritz.html) (University of Zurich)
- [Chris Satterfield](https://github.com/csatterfield) (contributor to MacOS version)
- [Roy Rutishauser](https://github.com/royru) (contributor to MacOS version)
- [Dr. Manuela Züger](http://www.ifi.uzh.ch/en/seal/people/zueger.html) (prev. University of Zurich)
- [Dr. Sebastian Müller](http://www.ifi.uzh.ch/en/seal/people/mueller.html) (prev. University of Zurich)
- [Dr. Tom Zimmermann](https://www.microsoft.com/en-us/research/people/tzimmer/) (Microsoft Research)
- [Prof. Dr. Gail C. Murphy](https://blogs.ubc.ca/gailcmurphy/) (University of British Columbia)

# Research
This tool was developed for and used by the following research:
- [CHI'20](https://andre-meyer.ch/CHI20) Supporting Software Developers’ Focused Work on Window-Based Desktops. Jan Pilzer, Raphael Rosenast. André Meyer. Elaine Huang. Thomas Fritz.
- [TSE'20](https://andre-meyer.ch/TSE20) Detecting Developers’ Task Switches and Types. André Meyer, Chris Satterfield, Manuela Züger, Katja Kevic, Gail Murphy, Thomas Zimmermann, and Thomas Fritz.
- [CSCW’18](https://www.andre-meyer.ch/CSCW18) Design Recommendations for Self-Monitoring in the Workplace: Studies in Software Development. André Meyer, Gail Murphy, Thomas Zimmermann, Thomas Fritz. (hint: in this paper, the tool described as WorkAnalytics refers to the PersonalAnalytics in this repository)
- [CHI’18](http://www.zora.uzh.ch/id/eprint/151128/1/pn4597-zugerA.pdf) Sensing Interruptibility in the Office: A Field Study on the Use of Biometric and Computer Interaction Sensors. Manuela Züger, Sebastian Müller, André Meyer, Thomas Fritz. 
- [TSE’17](https://www.andre-meyer.ch/TSE17) The Work Life of Developers: Activities, Switches and Perceived Productivity. André Meyer, Gail Murphy, Thomas Zimmermann, Laura Barton, Thomas Fritz. 
- [CHI’17](https://www.andre-meyer.ch/CHI17) Reducing Interruptions at Work: A Large-Scale Field Study of FlowLight. Manuela Züger, Christopher Corley, André Meyer, Boyang Li, Thomas Fritz, David Shepherd, Vinay Augustine, Patrick Francis, Nicholas Kraft and Will Snipes.

# Credits
We want to thank the following developers for providing us with the fantastic libraries:
- MouseKeyHook https://github.com/gmamaladze/globalmousekeyhook MIT License
- Hardcodet.NotifyIcon https://bitbucket.org/hardcodet/notifyicon-wpf Code Project Open License
- Jquery https://jquery.org/license/ MIT License
- Masonry https://github.com/desandro/masonry MIT License
- SQLite www.sqlite.org/copyright.html Open Domain 
- D3 Visualization https://github.com/mbostock/d3 BSD License
- C3.js https://github.com/masayuki0812/c3 MIT License 
- HTML FilterTable https://github.com/koalyptus/TableFilter MIT License
- Newtonsoft Json http://www.newtonsoft.com/json MIT License
- OpenNLP https://github.com/AlexPoint/OpenNlp MIT License 
- CocoaPods https://cocoapods.org/ MIT License
- EonilFSEvents https://github.com/eonil/FSEvents MIT License
- Sparkle https://sparkle-project.org/ MIT License
- create-dmg https://github.com/sindresorhus/create-dmg MIT License
- GRDB.swift https://github.com/groue/GRDB.swift MIT License
- Swifter https://github.com/httpswift/swifter MIT License
