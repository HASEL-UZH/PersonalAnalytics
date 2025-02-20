# PersonalAnalytics - Privacy-protecting, open-source Self-Monitoring Software

PersonalAnalytics is a self-monitoring software developed by the [Human Aspects of Software Engineering Lab](https://hasel.dev) of [the University of Zurich](https://www.uzh.ch) to non-intrusively collect computer interaction data and user's self-reports, store it locally on the user's machine, and allow users to voluntarily share a user-defined and potentially obfuscated subset of the data with researchers for scientific purposes.

The monitoring component captures computer interaction data including user input and app & website usage data. Every now and then, a self-reflection question asks the user a question, e.g. about the current stress level or perceived productivity. Since all data is stored locally only (and _not_ automatically uploaded to a server to avoid privacy concerns), a data export component helps the user to decide which data to share and allows it to be obfuscated, before securely sharing it with researchers. In the future, it will add a [retrospection]([url](https://www.andre-meyer.ch/CSCW18)) that will visualize and correlate the automatically collected and manually reported data to help users learn more about how they spend their time, their work habits and how it impacts productivity and time well spent. This software is open source, can be adapted and re-used for researchers' own scientific studies.

Anyone is welcome to contribute to PersonalAnalytics by extending it with new trackers or improving existing ones. Feel free to use PersonalAnalytics to get insights into your own work habits, or use it for research studies. In case you re-use PersonalAnalytics, ensure to provide proper [attribution as described here](./documentation/RESEARCH.md).


## 🧑‍💻 Installation & Usage as a User
Anyone may install PersonalAnalytics on their Windows or macOS device to non-intrusively collect computer interaction data, and analyze their activity, time spent and work habits for themselves. In the future, once we'll re-introduce the Retrospection (i.e. visualizations of the collected and self-reported data), it will be much easier to gain insights again.

Learn more about how to [install and use PersonalAnalytics](./documentation/INSTALLATION.md).

## 👩‍🔬 Customization & Usage as a Researcher
[![Build and Publish PersonalAnalytics](https://github.com/HASEL-UZH/PersonalAnalytics/actions/workflows/build.yml/badge.svg)](https://github.com/HASEL-UZH/PersonalAnalytics/actions/workflows/build.yml)

This project was created by and for researchers who want to ask study participants to run PersonalAnalytics on their device to non-intrusively collect **computer interaction data** in a privacy-protected way. As often times, having access to only automatically collected data is often not sufficient, PersonalAnalytics also offers an **experience sampling component**, which allows researchers to ask users to reflect and self-report on one or several questions (e.g. Have I been productive? Am I stressed right now?) at customizable times and using Likert-scales. As all collected data is only stored locally on participants' computers, there is an **export component**, guiding the participant through sharing and potentially obfuscating the captured data, before sharing it with the researchers through their data transfer service of choice. Most settings are configurable in the [study-config]([url](https://github.com/HASEL-UZH/PersonalAnalytics/blob/feature/electron/src/electron/shared/study.config.ts)), everything else can be customized in code.

Learn more about how to use [PersonalAnalytics for your research project](./documentation/RESEARCH.md).

## 📖 Further Information
- [Installation & Usage for End Users](./documentation/INSTALLATION.md)
- [Customization & Usage for Researchers](./documentation/RESEARCH.md)
- [Reporting an issue or bug](https://github.com/HASEL-UZH/PersonalAnalytics/issues)
- [Data Collection & Privacy Policy](./documentation/PRIVACY.md)
- [Contributions](./documentation/RESEARCH.md#contributions-guide)
- [Information on the old PersonalAnalytics](./documentation/LEGACY.md)
- Questions? Contact [Dr. André Meyer](mailto:ameyer@ifi.uzh.ch)


## 🕒 Maintenance Status
The original version of PersonalAnalytics for Windows (created by André Meyer) and for macOS (created by Roy Rutishauser and Chris Satterfield), and supported by several other contributors, have been deprecated and are no longer maintained. In case of interest, you can learn more about [PersonalAnalytics-legacy here](./documentation/LEGACY.md).

In 2024, we've revived the project in creating a multi-platform app using TypeScript and Electron. It is using the TypeScript-versions of our original, most used data trackers, the [WindowsActivityTracker](https://github.com/HASEL-UZH/PA.WindowsActivityTracker/tree/main/typescript) and the [UserInputTracker](https://github.com/HASEL-UZH/PA.UserInputTracker/tree/main/typescript). In addition, it includes an experience sampling component that can ask users to provide self-reports on one or several questions at customizable times. At the moment, the new PersonalAnalytics does NOT yet feature a retrospection, but it's the plan to recreate it in the future.


## 🙂 Main Contributors and People Involved
This work is carried by the following main contributors: 
- [Dr. André Meyer](https://www.andre-meyer.ch) (University of Zurich, main contributor to the project)
- [Prof. Dr. Thomas Fritz](http://www.ifi.uzh.ch/en/seal/people/fritz.html) (University of Zurich)
- [Sebastian Richner](https://github.com/SRichner) (contributor to new version)
- [Roy Rutishauser](https://github.com/royru) (contributor to MacOS-legacy version)
- [Chris Satterfield](https://github.com/csatterfield) (contributor to MacOS-legacy version)
- [Jan Pilzer](https://github.com/hirse) (contributor to Windows-legacy version)
- [Alexander Lill](https://github.com/alexanderlill) (tester)
- [Isabelle Cuber](https://github.com/isicu) (tester)
- Dr. Manuela Züger (prev. University of Zurich, contributor to Windows-legacy version)
- Dr. Sebastian Müller (prev. University of Zurich, contributor to Windows-legacy version)
- [Dr. Tom Zimmermann](https://www.microsoft.com/en-us/research/people/tzimmer/) (Microsoft Research)
- [Prof. Dr. Gail C. Murphy](https://blogs.ubc.ca/gailcmurphy/) (University of British Columbia)


## 📨 Contact
- You may contact André Meyer (ameyer@ifi.uzh.ch) in case of questions on the project.
- Do not attempt contact in case of questions on a specific study in which you are participating. If you encounter technical issues, create an [issue](https://github.com/HASEL-UZH/PersonalAnalytics/issues), so that the community may offer help.


## ↪️ Dependencies
This project uses [PA.WindowsActivityTracker](https://github.com/HASEL-UZH/PA.WindowsActivityTracker) and [PA.UserInputTracker](https://github.com/HASEL-UZH/PA.UserInputTracker/) as its main data trackers.
For the full list of dependencies, consider [package.json](./src/electron/package.json).
