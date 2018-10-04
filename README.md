# PersonalAnalytics - Building the Fitbit for Knowledge Workers
Personal Analytics project was originally initiated by [Prof. Dr. Thomas Fritz](http://www.ifi.uzh.ch/en/seal/people/fritz.html) and [André N. Meyer](https://www.andre-meyer.ch) from the SEAL Lab at the University of Zurich (UZH). Our goal is to build a self-monitoring tool that knowledge workers (e.g. developers, designers, administrators) install on their computer and that allows them to get insights into their work and productivity, and come up with positive behavior changes. The basis are a number of computer interaction trackers (e.g. application usage, emails/meetings, user input) and biometric trackers (e.g. Fitbit, Polar, Garmin, Muse, Tobii) that non-intrusively track data, store them locally on the users machine (to avoid privacy issues!) and then visualize them in a daily/weekly summary, the retrospection. 

Anyone is welcome to extend PersonalAnalytics with new trackers or improve existing ones. Feel free to use PersonalAnalytics to get insights into your own work habits, or use it for research studies. In case you re-use PersonalAnalytics, make sure to cite our work.

The current public release can be downloaded from: http://pluto.ifi.uzh.ch/PersonalAnalytics-master

# Contact
André Meyer (ameyer@ifi.uzh.ch)

# Updates & Branches
- September 2014: Initiated by [André Meyer](https://www.andre-meyer.ch) and regularly updated since then.
- November, 2015: OpenSourced the project (license: MIT).
- February, 2016: Improved the retrospection and added the Office 365 tracker during an internship at Microsoft Research between November, 2015 and February, 2016
- April, 2016: Merged the branch (from Microsoft Research) with the original version, following the open sourcing of the code.
- Mai, 2016: Started working on a communication dashboard (including more insights into interactions with others) with ABB Research (on a separate branch). This work has never been finished.
- June, 2016: Started working on integrating the Muse tracker with [Monica Rüegg](https://github.com/montrin) (master student at the University of Zurich, Switzerland) on the 'muse' branch. The branch has not stable enough to merge with master.
- December, 2016: Started working on integrating several other biometric sensors (Polar, Garmin, Fitbit) (on the 'biometrics' branch). The PolarTracker and GarminTracker are available and stable on the master branch. GarminTracker is still in development.
- February, 2017: Integrated the (privately developed) [FlowLight](https://www.andre-meyer.ch/flowlight) to avoid interruptions at inopportune moments. It has since then been removed from the repository, as it was licensed to [Embrava](https://embrava.com/pages/flow).
- March, 2017: Started working on task type detection (on the 'taskdetection' branch), still in development.
- June, 2018: [Rohit Kaushik](https://github.com/kaushik-rohit) (bachelor student at BITS Pilani, India) started working on a SlackTracker.
- September, 2018: [Chris Satterfield](https://github.com/csatterfield) and [Anna Scholtz](https://github.com/scholtzan) (master students at University of British Columbia, Canada) started integrating their port from Windows to Mac. Work in progress.
- September 2018: [Jan Pilzer](https://github.com/hirse) and [Raphael](https://github.com/raphaelro) started integrating their work on Tobii Eytracking. Work in progress.


# Collaborators & Contributors
- [André Meyer](https://www.andre-meyer.ch) (University of Zurich, main contributor of Windows version)
- [Prof. Dr. Thomas Fritz](http://www.ifi.uzh.ch/en/seal/people/fritz.html) (University of British Columbia)
- Dr. Tom Zimmermann (Microsoft Research)
- Prof. Dr. Gail C. Murphy (University of British Columbia)
- [Manuela Züger](http://www.ifi.uzh.ch/en/seal/people/zueger.html) and [Sebastian Müller](http://www.ifi.uzh.ch/en/seal/people/mueller.html) (University of Zurich)
- Paige Rodeghero, Boyang Li and Dave Shepherd (ABB Research)
- [Rohit Kaushik](https://github.com/kaushik-rohit) (responsible for SlackTracker)
- [Chris Satterfield](https://github.com/csatterfield) (main contributor of Mac version)


# Credits
We want to thank the following developers for providing us with the fantastic libraries:
- MouseKeyHook https://github.com/gmamaladze/globalmousekeyhook MIT License
- Hardcodet.NotifyIcon https://bitbucket.org/hardcodet/notifyicon-wpf 
- Jquery https://jquery.org/license/ MIT LIcense
- Masonry https://github.com/desandro/masonry MIT License
- SQLite www.sqlite.org/copyright.html Open Domain 
- D3 Visualization https://github.com/mbostock/d3 BSD License
- C3.js https://github.com/masayuki0812/c3 MIT License 
- HTML FilterTable https://github.com/koalyptus/TableFilter MIT license
- Newtonsoft Json http://www.newtonsoft.com/json MIT license
- OpenNLP https://github.com/AlexPoint/OpenNlp MIT license 
