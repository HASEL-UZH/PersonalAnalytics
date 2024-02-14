import { StudyConfig } from '../types/StudyConfig';

export const studyConfig: StudyConfig = {
  name: 'Personal Analytics',
  shortDescription: 'A study to understand how people...',
  infoUrl: 'https://hasel.dev',
  privacyPolicyUrl: 'https://hasel.dev/privacy',
  uploadUrl: 'https://hasel.dev/upload',
  contactName: 'Hasel Dev',
  contactEmail: 'study@hasel.dev',

  trackers: {
    windowActivityTracker: {
      enabled: true
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
