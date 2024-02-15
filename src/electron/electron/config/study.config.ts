import { StudyConfig } from '../types/StudyConfig';

const studyConfig: StudyConfig = {
  name: 'Personal Analytics',
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
      checkingForWindowChangeIntervalInMs: 1000
    },
    userInputTracker: {
      enabled: true
    },
    experienceSampling: {
      enabled: true,
      question: 'How are you feeling right now?',
      responseOptions: ['Bad', 'Neutral', 'Good'],
      samplingIntervalInMinutes: 60,
      samplingRandomization: true
    }
  }
};
export default studyConfig;
