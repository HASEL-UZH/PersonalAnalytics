# PersonalAnalytics for Researchers

This project was created by and for researchers who want to ask study participants to run PersonalAnalytics on their device to non-intrusively collect computer interaction data in a privacy-protected way. As often times, having access to only automatically collected data is often not sufficient, PersonalAnalytics also offers an experience sampling component, which allows researchers to ask users to reflect and self-report on one or several questions (e.g. Have I been productive? Am I stressed right now?) at customizable times and using Likert-scales. As all collected data is only stored locally on participants' computers, there is an export component, guiding the participant through sharing and potentially obfuscating the captured data, before sharing it with the researchers through their data transfer service of choice. Most settings are configurable in the study-config, everything else can be customized in code.


# Customizing PersonalAnalytics
To customize PersonalAnalytics for your research study, please consider the following steps:

<!-- tbd: Sebastian: update -->

1. Fork the project to work in your own repository.
2. Update the `study.config.ts`-[file](../src/electron/shared/study.config.ts) with your custom study-related settings. Hereby, you can add your custom study name, study title, privacy policy, export upload url as well as contact data. In addition, you can customize which computer interaction tracker isrunning, and if you want to prompt the user to self-report on one or several questions in the experience sampling component.
3. (optional) If you require further customizations, you can create them in the code (see [Contributions Guide](#contributions-guide)).  
4. Use Github Actions (see [build.yml](https://github.com/HASEL-UZH/PersonalAnalytics/blob/feature/electron/.github/workflows/build.yml)) to build and deploy PersonalAnalytics and allow your participants to use it. Using the method, PersonalAnalytics can automatically update your participants' installations with new releases

## General Configuration of PersonalAnalytics (edit in `study.config.ts`)
| Parameter           | Description                                                                                                                                                                                                                                                            | Change Required | Default Value |
|---------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------|---------------|
| `name`              | The name of the study. This is shown in various places of PersonalAnalytics, such as the PersonalAnalytics' menu icon tooltip, the experience sampling and when exporting the study data.                                                                              | ✅               |               |
| `shortDescription`  | A short description of the study. This is shown during the onboarding process, in the about page, and when exporting the study data.                                                                                                                                   | ✅               |               |
| `infoUrl`           | A URL to a website that provides more information about the study. This is shown during the onboarding process, in the about page and when exporting the study data.                                                                                                   | ✅               |               |
| `privacyPolicyUrl`  | A URL to a website that provides the privacy policy of the study. This is shown during the onboarding process, in the about page and when exporting the study data.                                                                                                    | ✅               |               |
| `uploadUrl`         | A URL to a service where participants can upload their study data. This is shown when exporting the study data.                                                                                                                                                        | ✅               |               |
| `contactName`       | The name of the person that participants can contact. This is shown during the onboarding process, in the about page, when exporting the study data, in case of errors and in the application's menu when requesting help or reporting an issue.                       | ✅               |               |
| `contactEmail`      | An email address that participants can use to contact the study organizers. This is shown during the onboarding process, in the about page, when exporting the study data, in case of errors and in the application's menu when requesting help or reporting an issue. | ✅               |               |
| `subjectIdLength`   | The length of the subject ID that is generated for each participant. This is used to identify the participant's data.                                                                                                                                                  |                 | `6`           |
| `dataExportEnabled` | Whether the participant should be able to export their data. If enabled, participants can export their study data through the application's menu.                                                                                                                      |                 | `true`        |

### Tracker Configuration (edit `trackers` in `study.config.ts`)
#### WindowsActivityTracker
| Parameter           | Description                                                                                                                                                                                                                                                                                                   | Change Required | Default Value |
|---------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------|---------------|
| `enabled`           | Whether the WindowsActivityTracker should be enabled. If enabled, the WindowsActivityTracker will collect data about the participant's application usage.                                                                                                                                                     |                 | `true`        |
| `intervalInMs`      | The interval in milliseconds at which the WindowsActivityTracker should collect data.                                                                                                                                                                                                                         |                 | `1000`        |
| `trackUrls`         | Whether the WindowsActivityTracker should track the URLs of the websites that the participant visits. On macOS: If this is enabled, participants need to enable the accessibility permission through the system settings. (They will be prompted to do so when running the application for the first time.)   |                 | `false`       |
| `trackWindowTitles` | Whether the WindowsActivityTracker should track the titles of the windows that the participant uses. On macOS: If this is enabled, participants need to enable the screen recording permission through the system settings. (They will be prompted to do so when running the application for the first time.) |                 | `true`        |

#### UserInputTracker
| Parameter           | Description                                                                                                                                          | Change Required | Default Value |
|---------------------|------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------|---------------|
| `enabled`           | Whether the UserInputTracker should be enabled. If enabled, the UserInputTracker will collect data about the participant's keyboard and mouse input. |                 | `true`        |
| `intervalInMs`      | The interval in milliseconds at which the UserInputTracker collects and stores the aggregated data.                                                  |                 | `10000`       |

#### ExperienceSamplingTracker
| Parameter               | Description                                                                                                                                                                                                                                                                                                                                    | Change Required | Default Value             |
|-------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------|---------------------------|
| `enabled`               | Whether the ExperienceSamplingTracker should be enabled. If enabled, the ExperienceSamplingTracker will prompt the participant to self-report on one or several questions at customizable times (see configuration below).                                                                                                                     |                 | `true`                    |
| `intervalInMs`          | The interval in milliseconds at which the ExperienceSamplingTracker should prompt the participant to self-report.                                                                                                                                                                                                                              |                 | `1000*60*60*3`  (3 hours) |
| `samplingRandomization` | Wether the ExperienceSamplingTracker should include a randomization for the time at which the participant is prompted to self-report. A value between 0 and 1. If enabled (value bigger than 0), the ExperienceSamplingTracker will randomly calculate a value that is between `intervalInMs` plus/minus `intervalInMs * samplingRanomization` |                 | `0.1`                     |
| `scale`                 | The questions scale. This applies to all questions.                                                                                                                                                                                                                                                                                            |                 | `7`                       |
| `questions`             | An array of questions that the participant should self-report on. Each question has a `text` and a `scale` property. The `text` property is the question that the participant should answer. The `scale` property is the scale that the participant should use to answer the question.                                                         | ✅               |                           |
| `responseOptions`       | An array of arrays with labels for the Likert-scale question. This can be either two labels that will be displayed on the left and right (e.g., `['strongly disagree', 'strongly agree']`) or three labels (e.g., `['strongly disagree', 'neutral', 'strongly agree']`).                                                                       | ✅               |                           |



// show a checkmark or a cross

# Contributions Guide
Anyone is welcome to contribute to PersonalAnalytics by extending it with new trackers or improving existing ones.

This quick guide helps you to set-up your development environment:
<!-- tbd: Sebastian -->


# Research that used PersonalAnalytics
PersonalAnalytics-legacy was used in the following peer-reviewed research projects (and other non-peer reviewed projects too, such as master and bachelor theses):
- [CHI'20](https://andre-meyer.ch/CHI20) Supporting Software Developers’ Focused Work on Window-Based Desktops. Jan Pilzer, Raphael Rosenast. André Meyer. Elaine Huang. Thomas Fritz.
- [TSE'20](https://andre-meyer.ch/TSE20) Detecting Developers’ Task Switches and Types. André Meyer, Chris Satterfield, Manuela Züger, Katja Kevic, Gail Murphy, Thomas Zimmermann, and Thomas Fritz.
- [CSCW’18](https://www.andre-meyer.ch/CSCW18) Design Recommendations for Self-Monitoring in the Workplace: Studies in Software Development. André Meyer, Gail Murphy, Thomas Zimmermann, Thomas Fritz. (hint: in this paper, the tool described as WorkAnalytics refers to the PersonalAnalytics in this repository)
- [CHI’18](http://www.zora.uzh.ch/id/eprint/151128/1/pn4597-zugerA.pdf) Sensing Interruptibility in the Office: A Field Study on the Use of Biometric and Computer Interaction Sensors. Manuela Züger, Sebastian Müller, André Meyer, Thomas Fritz. 
- [TSE’17](https://www.andre-meyer.ch/TSE17) The Work Life of Developers: Activities, Switches and Perceived Productivity. André Meyer, Gail Murphy, Thomas Zimmermann, Laura Barton, Thomas Fritz. 
- [CHI’17](https://www.andre-meyer.ch/CHI17) Reducing Interruptions at Work: A Large-Scale Field Study of FlowLight. Manuela Züger, Christopher Corley, André Meyer, Boyang Li, Thomas Fritz, David Shepherd, Vinay Augustine, Patrick Francis, Nicholas Kraft and Will Snipes.
