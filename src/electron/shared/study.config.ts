import { StudyConfiguration } from './StudyConfiguration'

const studyConfig: StudyConfiguration = {
  name: 'PersonalAnalytics Study',
  shortDescription:
    'PersonalAnalytics is a self-monitoring software developed by the Human Aspects of Software Engineering Lab of the University of Zurich to non-intrusively collect computer interaction data and store it locally on your computer. Every now and then, a self-reflection question asks you about time well spent and perceived productivity. In the future, it will add a retrospection that will visualize and correlate the automatically collected and manually reported data to help you learn more about how you spend your time and your productivity. This software is open source, can be adapted and re-used for your own scientific studies.',
  infoUrl: 'https://github.com/HASEL-UZH/PersonalAnalytics',
  privacyPolicyUrl:
    'https://github.com/HASEL-UZH/PersonalAnalytics/blob/dev-am/documentation/PRIVACY.md',
  uploadUrl: 'https://hasel.dev/upload',
  contactName: 'Dr. André Meyer',
  contactEmail: 'study@hasel.dev',
  subjectIdLength: 6,
  dataExportEnabled: true,
  dataExportEncrypted: false,
  displayDaysParticipated: true,
  trackers: {
    // ***AIRBAR - START
    taskTracker: {
      enabled: true
    },
    // ***AIRBAR - END
    windowActivityTracker: {
      enabled: true,
      intervalInMs: 1000,
      trackUrls: false,
      trackWindowTitles: true
    },
    userInputTracker: {
      enabled: true,
      intervalInMs: 10000
    },
    experienceSamplingTracker: {
      enabled: true,
      enabledWorkHours: true,
      scale: 7,
      questions: [
        'Compared to your normal level of productivity, how productive do you consider the previous session?',
        'How well did you spend your time in the previous session?'
      ],
      responseOptions: [
        ['not at all productive', 'moderately productive', 'very productive'],
        ['not well', 'moderately well', 'very well']
      ],
      // TODO: Change back to 3 hours
      intervalInMs: 1000 * 60 * 60 * 1,
      // 10% randomization, so the interval will be between 2.7 and 3.3 hours
      samplingRandomization: 0.1
    }
  }
}
export default studyConfig
