# Architecture Overview `Work in Progress`

This document provides a brief overview over the architecture of PersonalAnalytics. It is still work in progress.

## Supported Platforms
At the moment, the following platforms are supported and PersonalAnalytics is extensively tested on them:
- Windows 8.1 and Windows 10 (Windows 7 is no longer supported) [Link](https://github.com/HASE-UZH/PersonalAnalytics/tree/dev-am/src/windows/)
- macOS 10.12 or newer [Link](https://github.com/HASE-UZH/PersonalAnalytics/tree/dev-am/src/macOS/)
In the future, the aim is to also provide similar apps for mobile (Android and iOS) and Linux. Contributions are welcome.

## Basic Concepts
- PersonalAnalytics features a number of Data Trackers, each responsible for tracking, aggregating and storing a certain type of data (e.g. user input).
- PersonalAnalytics was built with extensibility in mind, meaning that it is very easy to add a new Data Tracker to PersonalAnalytics and dynamically enable/disable them, depending on the usage scenarios (e.g. private usage or research project).

## Data Trackers
`TODO: complete description and table`
- Data Trackers can be enabled or disabled
- Data Trackers implement the `ITracker` interface
- Visualizations are only provided in the retrospection for enabled trackers

| Data Tracker           | Collected Data | Database Table Name | OS Support | Maturity |
|------------------------|----------------|---------------------|------------|----------|
| WindowsActivityTracker | for each application used, the time, duration, process name and window title is stored | windows_activity | Windows, macOS | frequently used |
| UserInputTracker       | mouse movement, clicks, scrolls; keyboard strokes (not actual keys, only type (any, navigate, delete) | user_input | Windows, macOS | frequently used |
| MsOfficeTracker        | Email info (number of emails in inbox, sent, received, unread), meeting info (time, subject, duration, number of attendees) | emails, meetings | Windows | frequently used |
| UserEfficiencyTracker  | participants' self-reports in interval pop-up (e.g. perceived productivity) | user_efficiency_survey, user_efficiency_survey_day | Windows, macOS | frequently used |
| FitbitTracker          | synced data, such as heart rate | fitbit | Windows | not actively tested for 1-2 years |
| PolarTracker          | synced data, such as heart rate | polar | Windows | not actively tested for 1-2 years |

`Note:` The project is currently not maintained super actively. However, the most important data trackers were ported to run on both `Windows` and `macOS` using `Typescript` and some OS-native code. They are OSS, actively used and maintained here:
- [WindowsActivityTracker](https://github.com/HASEL-UZH/PA.WindowsActivityTracker/tree/main/typescript) that allows to log the timestamp, app name and window title of the currently active window, in addition to an automated catgorization into the `Activity` (method described in this [publication](https://andre-meyer.ch/TSE20).
- [UserInputTracker](https://github.com/HASEL-UZH/PA.UserInputTracker/tree/main/typescript) that allows to log moue movement, clicks, scrolls and keystrokes with timestamps (not actual keys, only the type (any, navigate, delete) for privacy reasons)

## Retrospection
`TODO: describe basics and functionality of retrospection`


![Retrospection Screenshot](./images/retrospection_screenshot.png?raw=true)
