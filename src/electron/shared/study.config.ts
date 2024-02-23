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
      intervalInMs: 1000
    },
    userInputTracker: {
      enabled: true,
      intervalInMs: 60000
    },
    experienceSampling: {
      enabled: true,
      scale: 7,
      questions: [
        'Compared to your normal level of productivity, how productive do you consider the previous session?'
      ],
      responseOptions: [['not at all productive', 'moderately productive', 'very productive']],
      samplingIntervalInMinutes: 60,
      samplingRandomization: true
    }
  }
};
export default studyConfig;
