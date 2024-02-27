import { StudyConfiguration } from './StudyConfiguration';

const studyConfig: StudyConfiguration = {
  name: 'Personal Analytics Study',
  shortDescription: 'A study to understand how people...',
  infoUrl: 'https://hasel.dev',
  privacyPolicyUrl: 'https://hasel.dev/privacy',
  uploadUrl: 'https://hasel.dev/upload',
  contactName: 'Hasel Dev',
  contactEmail: 'study@hasel.dev',
  subjectIdLength: 6,
  trackers: {
    windowActivityTracker: {
      enabled: true,
      intervalInMs: 1000,
      trackUrls: false,
      trackWindowTitles: true
    },
    userInputTracker: {
      enabled: true,
      intervalInMs: 60000
    },
    experienceSamplingTracker: {
      enabled: true,
      scale: 7,
      questions: [
        'Compared to your normal level of productivity, how productive do you consider the previous session?'
      ],
      responseOptions: [['not at all productive', 'moderately productive', 'very productive']],
      // 3 hours
      intervalInMs: 1000 * 60 * 60 * 3,
      // 10% randomization, so the interval will be between 2.7 and 3.3 hours
      samplingRandomization: 0.1
    }
  }
};
export default studyConfig;
