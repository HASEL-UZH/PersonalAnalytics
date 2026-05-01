import { StudyConfiguration } from './StudyConfiguration';
import { DataExportFormat } from './DataExportFormat.enum';

const studyConfig: StudyConfiguration = {
  name: 'PersonalAnalytics',
  shortDescription:
    'PersonalAnalytics is a self-monitoring software developed by the Human Aspects of Software Engineering Lab of the University of Zurich to non-intrusively collect computer interaction data and store it locally on your computer. Every now and then, a self-reflection question asks you about time well spent and perceived productivity. In the future, it will add a retrospection that will visualize and correlate the automatically collected and manually reported data to help you learn more about how you spend your time and your productivity. This software is open source, can be adapted and re-used for your own scientific studies.',
  infoUrl: 'https://github.com/HASEL-UZH/PersonalAnalytics',
  privacyPolicyUrl:
    'https://github.com/HASEL-UZH/PersonalAnalytics/blob/dev/documentation/PRIVACY.md',
  uploadUrl: 'https://hasel.dev/upload',
  contactName: 'Dr. André Meyer',
  contactEmail: 'study@hasel.dev',
  subjectIdLength: 6,
  dataExportEnabled: true,
  dataExportFormat: DataExportFormat.ExportAsZippedSqlite,
  dataExportEncrypted: false,
  displayDaysParticipated: true,
  showActiveTimesInOnboarding: true,
  enableRetrospection: true,
  trackers: {
    windowActivityTracker: {
      enabled: true,
      intervalInMs: 1000,
      trackUrls: false,
      trackWindowTitles: true
    },
    userInputTracker: {
      enabled: true,
      intervalInMs: 60000,
      collectKeyDetails: true
    },
    experienceSamplingTracker: {
      enabled: true,
      enabledWorkHours: true,
      questions: [
        {
          question:
            'Compared to your normal level of productivity, how productive do you consider the previous session?',
          answerType: 'LikertScale',
          scale: 7,
          responseOptions: ['not at all productive', 'moderately productive', 'very productive']
        },
        {
          question: 'How well did you spend your time in the previous session?',
          answerType: 'LikertScale',
          scale: 5,
          responseOptions: ['not well', 'moderately well', 'very well']
        },
        // {
        //   question: 'What is one aspect that affected your ability to focus the most in the last session?',
        //   answerType: 'TextResponse',
        //   responseOptions: 'singleLine',
        //   maxLength: 100
        // },
        {
          question: 'What best describes your current task type?',
          answerType: 'SingleChoice',
          responseOptions: ['Coding', 'Reading/Writing Documents', 'Meeting', 'Planning', 'Email & Chat Communication', 'Learning', 'Other']
        },
        // {
        //   question: 'Which distractions did you experience in the last session?',
        //   answerType: 'MultiChoice',
        //   responseOptions: ['Notifications', 'Meetings', 'Context switching', 'Personal interruptions', 'None']
        // }
      ],
      intervalInMs: 1000 * 60 * 60 * 1, // default interval (must be listed in userDefinedInterval_h if set)
      samplingRandomization: 0.2, // 20% randomization, so the interval will be between 48 and 72 minutes
      allowUserToDisable: true,
      allowUserToChangeInterval: true,
      userDefinedInterval_h: [0.5, 1, 2, 3, 4]
    },
    dailySurveyTracker: {
      enabled: true,
      requireAllAnswers: false,
      surveys: [
        {
          samplingType: 'morning',
          delayInMinutes: 5,
          questions: [
            {
              question: 'How motivated are you to start today?',
              answerType: 'LikertScale',
              scale: 7,
              responseOptions: ['not at all motivated', 'moderately motivated', 'very motivated']
            },
            {
              question: 'What is your main goal for today?',
              answerType: 'TextResponse',
              responseOptions: 'singleLine',
              maxLength: 150
            }
          ]
        },
        {
          samplingType: 'evening',
          delayInMinutes: -30,
          questions: [
            {
              question: 'Overall, how satisfied are you with your workday?',
              answerType: 'LikertScale',
              scale: 5,
              responseOptions: ['very satisfied', 'satisfied', 'neutral', 'dissatisfied', 'very dissatisfied']
            },
            {
              question: 'How much did you interact with your co-workers today?',
              answerType: 'SingleChoice',
              responseOptions: ['not at all', 'rarely', 'sometimes', 'often', 'all the time']
            },
            {
              question: 'Where did you mostly work from?',
              answerType: 'SingleChoice',
              responseOptions: ['mostly at the office', 'mostly remotely', 'mostly at home']
            },
            {
              question: 'Which distractions did you experience today?',
              answerType: 'MultiChoice',
              responseOptions: ['Notifications', 'Unplanned meetings', 'Co-worker interruptions', 'Context switching', 'Noisy environment', 'Personal matters', 'None']
            }
          ]
        }
      ]
    }
  }
};

export default studyConfig;
