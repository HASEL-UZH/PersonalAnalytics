# PersonalAnalytics for Researchers

This project was created by and for researchers who want to ask study participants to run PersonalAnalytics on their device to non-intrusively collect computer interaction data in a privacy-protected way. As often times, having access to only automatically collected data is often not sufficient, PersonalAnalytics also offers an experience sampling component, which allows researchers to ask users to reflect and self-report on one or several questions (e.g. Have I been productive? Am I stressed right now?) at customizable times and using Likert-scales. As all collected data is only stored locally on participants' computers, there is an export component, guiding the participant through sharing and potentially obfuscating the captured data, before sharing it with the researchers through their data transfer service of choice. Most settings are configurable in the study-config, everything else can be customized in code.


## Customizing PersonalAnalytics
To customize PersonalAnalytics for your research study, please consider the following steps:

1. Fork the project to work in your own repository.
2. Update the [study configuration-file](../src/electron/shared/study.config.ts) (`study.config.ts`) with your custom study-related settings (see details below). Hereby, you can add your custom study name, study title, privacy policy, export upload url as well as contact data. In addition, you can customize which computer interaction tracker is running, and if you want to prompt the user to self-report on one or several questions in the experience sampling component.
3. (optional) If you require further customizations, you can create them in the code (see [Contributions Guide](#contributions-guide)).  
4. Use GitHub Actions (see [build.yml](https://github.com/HASEL-UZH/PersonalAnalytics/blob/feature/electron/.github/workflows/build.yml)) to build and deploy PersonalAnalytics and allow your participants to use it. Using the method, PersonalAnalytics can automatically update your participants' installations with new releases

When creating new releases, update the package.json file with the new version number and commit and push it.

### Required GitHub Secrets
To use GitHub Actions to build and create PersonalAnalytics releases, you need to set the following secrets in your repository:

- `GH_TOKEN`: a GitHub token with the `repo` scope (see [Access Tokens](https://github.com/settings/tokens))
- Windows Code Signing secrets ([see below](
https://github.com/HASEL-UZH/PersonalAnalytics/blob/dev/documentation/RESEARCH.md#windows-secrets-add-to-github-secrets))
- macOS Code Signing secrets ([see below](https://github.com/HASEL-UZH/PersonalAnalytics/blob/dev/documentation/RESEARCH.md#code-signing-for-macos))

### Required Changes in `electron-builder.config.cjs`
These changes are required to automatically publish the built artifacts to GitHub releases. You need to replace the `owner` and `repo` with your GitHub username and repository name.
You can find more information on electron-builder here: https://www.electron.build/ and for the `electron-builder.config.cjs` file here: https://www.electron.build/configuration/configuration

```cjfs
{
  publish: {
    provider: "github",
    owner: "YOUR_GITHUB_USER_OR_ORGANIZATION",
    repo: "YOUR_REPOSITORY_NAME",
  }
}
```

### General Configuration of PersonalAnalytics (edit in `study.config.ts`)
| Parameter           | Description                                                                                                                                                                                                                                                            | Change Required | Default Value |
|---------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------|---------------|
| `name`              | The name of the study. It is shown in various places of PersonalAnalytics, such as the the about page, the experience sampling and when exporting the study data.                                                                              | ✅               |               |
| `shortDescription`  | A short description of the study. It is shown during the onboarding process, in the about page, and when exporting the study data. It should describe the study goal and summarize the collected data and how the data is analyzed.                                                                                                                                   | ✅               |               |
| `infoUrl`           | A link to a website (starting with `https://`) to provide additional details about the study. It is shown during the onboarding process, in the about page and when exporting the study data.                                                                                                   | ✅               |               |
| `privacyPolicyUrl`  | A link to a website (starting with `https://`) that describes the privacy policy of the study. It is shown during the onboarding process, in the about page and when exporting the study data.                                                                                                    | ✅               |               |
| `uploadUrl`         | A link to a website (starting with `https://`) that offers file-uploads for the participants to share their study data (e.g. SharePoint, Dropbox, Dropfiles website). It is shown when exporting the study data.                                                                                                                                                        | ✅               |               |
| `contactName`       | The name of the Principal Investigator (PI) that participants can contact. It is shown during the onboarding process, in the about page, when exporting the study data, in case of errors and in the application's menu when requesting help or reporting an issue.                       | ✅               |               |
| `contactEmail`      | An email address that participants can use to contact the researchers. It is shown during the onboarding process, in the about page, when exporting the study data, in case of errors and in the application's menu when requesting help or reporting an issue. | ✅               |               |
| `subjectIdLength`   | The length of the subject ID that is automatically generated for each participant when PersonalAnalytics is installed (e.g. `8JE7DA`).                                                                                                                                                  |                 | `6`           |
| `dataExportEnabled` | Whether the participant should be able to export their data. If enabled, participants can export their study data through the context menu.                                                                                                                      |                 | `true`        |
| `displayDaysParticipated` | Counts the number of days participants participants are active in the current study and visualizes the result as `Days participated: X` in the context menu in the task tray (Windows) or menubar (macOS). A day counts as participated if it includes (a) at least one experience sampling response (if enabled) and (b) at least 60 user input entries (indicating 60+ minutes of activity), as defined [here](../src/electron/electron/main/services/trackers/DaysParticipatedTracker.ts). |                 | `true`        |

### Tracker Configuration (edit `trackers` in `study.config.ts`)
#### WindowsActivityTracker
| Parameter           | Description                                                                                                                                                                                                                                                                                                   | Change Required | Default Value |
|---------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------|---------------|
| `enabled`           | Whether the WindowsActivityTracker is enabled. If enabled, the WindowsActivityTracker will collect data about the participant's application usage, including app names, window titles, visited URLs etc.                                                                                                                                                     |                 | `true`        |
| `intervalInMs`      | The interval in milliseconds at which the WindowsActivityTracker should collect data. We recommend to keep the default.                                                                                                                                                                                                                        |                 | `1000`        |
| `trackUrls`         | Whether the WindowsActivityTracker should track the URLs of the websites that the participant visits. This feature only works on macOS. When enabled, participants need to enable the accessibility permission through the system settings, and will automatically be prompted to do so when running the application for the first time.   |                 | `false`       |
| `trackWindowTitles` | Whether the WindowsActivityTracker should track the titles of the windows that the participant uses. On macOS and when enabled, participants need to enable the screen recording permission through the system settings, and will automatically be prompted to do so when running the application for the first time. |                 | `true`        |

#### UserInputTracker
| Parameter           | Description                                                                                                                                          | Change Required | Default Value |
|---------------------|------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------|---------------|
| `enabled`           | Whether the UserInputTracker is enabled. If enabled, the UserInputTracker will collect data about the participant's keyboard and mouse input (number of keystrokes, number of clicks, pixels moved and pixels scrolled in the defined interval). |                 | `true`        |
| `intervalInMs`      | The interval in milliseconds at which the UserInputTracker collects and stores the aggregated data.                                                  |                 | `10000`       |

#### ExperienceSamplingTracker
| Parameter               | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            | Change Required | Default Value             |
|-------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------|---------------------------|
| `enabled`               | Whether the ExperienceSamplingTracker is enabled. If enabled, the ExperienceSamplingTracker will prompt the participant to self-report on one or several questions at customizable times (see configuration below).                                                                                                                                                                                                                                                                                    |                 | `true`                    |
| `enabledWorkHours`      | Whether the ExperienceSamplingTracker allows participants to define their work hours for experience sampling. Participants can set or disable their work hours under 'Settings > Active Times'. By default, work hours are set from 8 AM to 5 PM on weekdays.                                                                                                                                                                                                                                           |                 | `true`                    |
| `intervalInMs`          | The interval in milliseconds at which the ExperienceSamplingTracker should prompt the participant to self-report.                                                                                                                                                                                                                                                                                                                                                                                      |                 | `1000*60*60*3`  (3 hours) |
| `samplingRandomization` | Whether the ExperienceSamplingTracker should include a randomization for the time at which the participant is prompted to self-report. A value between 0 and 1. If enabled (value bigger than 0), the ExperienceSamplingTracker will randomly calculate a value that is between `intervalInMs` plus/minus `intervalInMs * samplingRanomization`. If the `intervalInMs` is set to `1000 * 60 * 60 * 3` (3 hours) and the `samplingRanomization` to `0.1`, the prompt may be shown in 162 - 198 minutes. |                 | `0.1`                     |
| `scale`                 | The number of items of the Likert-scale. Per the definition, it should be an odd number, ideally between 3-9. It applies to all questions.                                                                                                                                                                                                                                                                                                                                                             |                 | `7`                       |
| `questions`             | An array of questions that the participant should self-reflect on. You can define one or multiple questions within the array (e.g., `['I am more productive in my current work session compared to the last one.']`). If the array consists of multiple questions, the question will be randomly selected.                                                                                                                                                                                             | ✅               |                           |
| `responseOptions`       | An array of arrays with labels for the Likert-scale question. This can be either two labels that will be displayed on the left and right (e.g., `['strongly disagree', 'strongly agree']`) or three labels (e.g., `['strongly disagree', 'neutral', 'strongly agree']`).  The same order as defined for the `questions` applies.                                                                                                                                                                                                                              | ✅               |                           |


## Contributions Guide
Anyone is welcome to contribute to PersonalAnalytics, for example by fixing bugs, extending it with new trackers or improving existing ones.

1. Fork the project to work in your own repository.
2. Create a new branch for your changes.
3. Make your changes and commit them to your branch.
4. Push your branch to your fork.
5. Create a pull request from your branch to the `main` branch of the main repository.
6. Wait for the maintainers to review your pull request.
7. If your pull request is approved, it will be merged into the main repository.
8. If your pull request is not approved, you can make further changes and push them to your branch. The pull request will be updated automatically.

If you have any questions, please contact Dr. André Meyer (ameyer@ifi.uzh.ch).

### Install the dependencies
Start with cloning the repository:
```bash
git clone https://github.com/HASEL-UZH/PersonalAnalytics
```

As the repository includes submodules, run:
```bash
git submodule init
git submodule update
```

After cloning this repository using your favorite git client, you need to install the dependencies.
Make sure you use node version >=20. You can install the dependencies by running the following command in the root directory of the project:
```bash
cd src/electron
npm install
```
This will install all the dependencies required to build and run PersonalAnalytics. This will also call the `postinstall` script, which will make sure that the native dependencies are built for your platform.

### Starting PersonalAnalytics for development/debugging
To start the application for development, you can run the following command in `src/electron`:
```bash
npm run dev
```

### Building PersonalAnalytics
To build the application, you can run the following command in `src/electron`:
```bash
npm run build
```
This will build the application for your platform and architecture. The built application will be located in the `release` directory.


You can also run the following command to build the application for Windows:
```bash
npm run build:win
```
or for macOS (only on macOS):
```bash
npm run build:mac
```

### Releasing PersonalAnalytics
Note that we're using Electron Builder and Github Actions (see [docu]([url](https://www.electron.build/publish.html#recommended-github-releases-workflow))).

- Increase the version number in `src/electron/package.json`
- Create a _draft_ release in the repository under /releases (details)
  - title: version number (format: `m.m.m`, e.g. 0.0.23)
  - tag: prefix version number with with `v` (format: `vm.m.m`, e.g. v0.0.23)
  - summarize release notes
  - save as draft (!)
- Push the commit to main (via PR)
- Github action is automatically triggered, wait for successful completion
- Manually download and test the release, and if all is good, publish it!
- Releases are found under https://github.com/HASEL-UZH/PersonalAnalytics/releases

### Code Signing for Windows

For Windows, we are using Azure Trusted Signing (beta) for code signing using Andre's personal account.
Please refer to the [electron-builder documentation](https://www.electron.build/code-signing.html#using-with-azure-trusted-signing-beta)
and [Azure's documentation](https://learn.microsoft.com/en-us/azure/trusted-signing/how-to-signing-integrations)
for more information. Following [this guide](https://melatonin.dev/blog/code-signing-on-windows-with-azure-trusted-signing/), the following
secrets and variables were defined for the GitHub action:


#### Windows Secrets (add to Github Secrets)

| Name                      | Description                                                                                                                              |
|---------------------------|------------------------------------------------------------------------------------------------------------------------------------------|
| `AZURE_TENANT_ID`         | The Microsoft Entra tenant (directory) ID.                                                                                               |
| `AZURE_CERT_PROFILE_NAME` | The name of the certificate (public trust) profile.                                                                                      |
| `AZURE_CLIENT_ID`         | The client (application) ID of an App Registration in the tenant.                                                                        |
| `AZURE_CLIENT_SECRET`     | The client secret that was generated for the App Registration.                                                                           |
| `AZURE_CODE_SIGNING_NAME` | The name of the trusted signing account (from the main Trusted Signing Account page in Azure).                                           |
| `AZURE_ENDPOINT`          | The URL for the selected region, labelled as Account URI (from the main Trusted Signing Account page in Azure).                          |
| `AZURE_PUBLISHER_NAME`    | The publisher name used for code signing for Windows builds. This is the full string in the form of `CN=..., O=..., L=..., S=..., C=CH`. |

Note that using special characters (e.g. `é`) in the publisher name can lead to issues with the code signing process.

The above secrets and variables are used in the action files (e.g., [build.yml](https://github.com/HASEL-UZH/PersonalAnalytics/blob/dev/.github/workflows/build.yml)) and
provided to the electron-builder ([electron-builder.config.cjs](../src/electron/electron-builder.config.cjs)) via environment variables.


### Code Signing for macOS

For macOS, the electron-builder's code signing is
disabled ([electron-builder.config.cjs](../src/electron/electron-builder.config.cjs)) and we are using the `afterSign` hook to
call the [notarize.cjs](../src/electron/scripts/notarize.cjs) script to sign the application using the `@electron/notarize`
package. The following environment variables are required for the code signing process:


#### macOS Secrets (add to Github Secrets)

| Name                          | Description                                                                                         |
|-------------------------------|-----------------------------------------------------------------------------------------------------|
| `APPLE_ID`                    | The Apple ID used for notarization.                                                                 |
| `APPLE_ID_PASS`               | The password for the Apple ID.                                                                      |
| `APPLE_TEAM_ID`               | The Apple Team ID (https://developer.apple.com/help/account/manage-your-team/locate-your-team-id/). |
| `APPLE_APP_SPECIFIC_PASSWORD` | App-Specific Password linked to your Apple ID (https://appleid.apple.com/account/manage).           |
| `CSC_LINK`                    | base64-encoded data of the Apple Developer Account certificate.                                     |
| `CSC_KEY_PASSWORD`            | Password of the Apple Developer Account certificate used to decrypt the certificate.                |

### Testing PersonalAnalytics
PersonalAnalytics was tested on `Windows 11` and `macOS 14`. It might work on older versions as well.

## Referencing PersonalAnalytics
When leveraging PersonalAnalytics for your work or research, please cite it appropriately, by refering to the main publication as well as this repository.

Citing the paper:
`Meyer, A. N., Murphy, G. C., Zimmermann, T., & Fritz, T. (2017). Design recommendations for self-monitoring in the workplace: Studies in software development. Proceedings of the ACM on Human-Computer Interaction, 1(CSCW), 1-24. https://doi.org/10.1145/3134714`

Citing the repository:
`https://github.com/HASEL-UZH/PersonalAnalytics`

## Research that used PersonalAnalytics
PersonalAnalytics-legacy was used in the following peer-reviewed research projects (and other non-peer reviewed projects too, such as master and bachelor theses):
- [CSCW'25](https://hasel.dev/wp-content/uploads/2024/12/2025_FlowTeams_CSCW25_PrePrint.pdf]) Better Balancing Focused Work and Collaboration in Hybrid Teams by Cultivating the Sharing of Work Schedules. André Meyer. Thomas Fritz.
- [CHI'20](https://andre-meyer.ch/CHI20) Supporting Software Developers’ Focused Work on Window-Based Desktops. Jan Pilzer, Raphael Rosenast. André Meyer. Elaine Huang. Thomas Fritz.
- [TSE'20](https://andre-meyer.ch/TSE20) Detecting Developers’ Task Switches and Types. André Meyer, Chris Satterfield, Manuela Züger, Katja Kevic, Gail Murphy, Thomas Zimmermann, and Thomas Fritz.
- [CSCW’18](https://www.andre-meyer.ch/CSCW18) Design Recommendations for Self-Monitoring in the Workplace: Studies in Software Development. André Meyer, Gail Murphy, Thomas Zimmermann, Thomas Fritz. (hint: in this paper, the tool described as WorkAnalytics refers to the PersonalAnalytics in this repository)
- [CHI’18](http://www.zora.uzh.ch/id/eprint/151128/1/pn4597-zugerA.pdf) Sensing Interruptibility in the Office: A Field Study on the Use of Biometric and Computer Interaction Sensors. Manuela Züger, Sebastian Müller, André Meyer, Thomas Fritz. 
- [TSE’17](https://www.andre-meyer.ch/TSE17) The Work Life of Developers: Activities, Switches and Perceived Productivity. André Meyer, Gail Murphy, Thomas Zimmermann, Laura Barton, Thomas Fritz. 
- [CHI’17](https://www.andre-meyer.ch/CHI17) Reducing Interruptions at Work: A Large-Scale Field Study of FlowLight. Manuela Züger, Christopher Corley, André Meyer, Boyang Li, Thomas Fritz, David Shepherd, Vinay Augustine, Patrick Francis, Nicholas Kraft and Will Snipes.

## Questions & Support
Please contact André Meyer (ameyer@ifi.uzh.ch) in case of questions.
