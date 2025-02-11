import { StudyConfiguration } from './StudyConfiguration';

const studyConfig: StudyConfiguration = {
  name: 'SWELL - Student Wellbeing and Learning on Laptops',
  shortDescription:
    'tbd',
  infoUrl: 'https://mydata-lab.uzh.ch/de/studien/SWELL.html',
  privacyPolicyUrl: 'https://mydata-lab.uzh.ch/de/studien/SWELL.html',
  uploadUrl: 'https://hasel.dev/swell-upload',
  contactName: 'Dr. Malte Doehne, Andreas Baumer, Dr. Andre Meyer',
  contactEmail: 'swell@d2usp.ch',
  subjectIdLength: 6,
  dataExportEnabled: true,
  dataExportEncrypted: false,
  trackers: {
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
      scale: 7,
      questions: [
        'Compared to your normal level of productivity, how productive do you consider the previous session?',
        'How well did you spend your time in the previous session?'
      ],
      responseOptions: [
        ['not at all productive', 'moderately productive', 'very productive'],
        ['not well', 'moderately well', 'very well']
      ],
      intervalInMs: 1000 * 60 * 60 * 3, // 3 hours
      // 10% randomization, so the interval will be between 2.7 and 3.3 hours
      samplingRandomization: 0.1
    }
  }
};
export default studyConfig;
