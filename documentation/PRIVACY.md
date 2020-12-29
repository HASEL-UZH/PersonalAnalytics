# Data Storage & Confidentiality

PersonalAnalytics is a Windows and macOS application designed to be installed on knowledge workers computers to non-intrusively collect data about their work and work habits, including application usage, user input, websites visited, files worked on, emails and meetings, amongst others.

All data (tracked and self-reported data) is *stored ONLY locally on the knowledge worker's computer*. It is NOT uploaded to any servers. This also includes stored credentials, e.g. of the Microsoft Office 365 service or Fitbit service. When running, the context menu of the app has a menu item "Open Collected Data" which opens the directory where all the collected data is stored. In most cases, the directory contains a log file (text-file), a database file (pa.dat) and the hashed token for accessing the Microsoft Office 365 service. 

To manually review, modify and delete the collected data, the user needs to open the `pa.dat` file using [SQLite Browser](https://sqlitebrowser.org/).

In the settings, the user can enable or disable the different data trackers.

During research studies, the researchers might ask users (i.e. study participants) to share the collected data with the researchers. They might or might not offer to obfuscate the collected data beforehand. This step depends entirely on the researchers and the owners and contributors of this project have no control and/or responsibility over this step. They encourage any researchers who use PersonalAnalytics for their research projects, to (1) be very transparent with what data is collected, (2) what the collected data will be used for (once the participants shares it with the researchers), (3) to only collect the absolute minimum of data, (4) to provide participants with a consent form to allow them to give informed consent on the collected data and its usage, and (5) to allow participants to require the deletion of their data.
