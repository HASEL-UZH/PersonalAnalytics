import { StudyConfiguration } from './StudyConfiguration';
import { DataExportFormat } from './DataExportFormat.enum';

const studyConfig: StudyConfiguration = {
  name: 'D2CARE | Computer Analytics for Reflection and Engagement',
  shortDescription:
    '<p><strong>Welcome to D²CARE.</strong></p><p>Developed at the University of Zurich, our privacy-first software helps you reflect on your digital workday by self-monitoring app usage (start/end times, app names), aggregated input activity (keyboard/mouse patterns, not what you type), and brief mood check-ins via experience sampling - all stored securely and only on your device. Research shows that reflecting on these patterns can already lead to greater awareness and meaningful improvements in digital habits.</p><p>We may occasionally reach out to invite you to anonymously donate your usage data and take part in short surveys as part of our non-commercial research on digital behavior and wellbeing. Participation is always voluntary and typically includes a small token of appreciation. All data donated to us is handled with strict confidentiality and care. </p><p>If you have questions, feedback, or ideas for how D²CARE could support other non-commercial research, we’d love to hear from you.</p><p><strong>D²CARE - Self-Monitor with CARE.</strong></p>',
  infoUrl: 'https://www.mydata-lab.uzh.ch/d2care.html',
  privacyPolicyUrl: 'https://www.mydata-lab.uzh.ch/d2care/privacy.html',
  uploadUrl: '',
  contactName: 'Dr. Malte Doehne and Dr. André Meyer',
  contactEmail: 'd2care@d2usp.ch',
  subjectIdLength: 7,
  dataExportEnabled: true,
  dataExportFormat: DataExportFormat.ExportToDDL,
  dataExportEncrypted: false,
  displayDaysParticipated: true,
  trackers: {
    windowActivityTracker: {
      enabled: true,
      intervalInMs: 1000,
      trackUrls: true,
      trackWindowTitles: true
    },
    userInputTracker: {
      enabled: true,
      intervalInMs: 60000
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
      intervalInMs: 1000 * 60 * 60 * 5, // 5 hours
      samplingRandomization: 0.1
    }
  }
};
export default studyConfig;
