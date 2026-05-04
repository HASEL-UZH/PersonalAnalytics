import { DataExportFormat } from './DataExportFormat.enum';

export interface UserInputTrackerConfiguration {
  enabled: boolean;
  intervalInMs: number;
  // Optional flag for detailed key category counts (defaults to false).
  collectKeyDetails?: boolean;
}

export interface WindowActivityTrackerConfiguration {
  enabled: boolean;
  intervalInMs: number;
  trackUrls: boolean;
  trackWindowTitles: boolean;
}

export interface ExperienceSamplingTrackerConfiguration {
  enabled: boolean;
  enabledWorkHours: boolean;
  questions: ExperienceSamplingQuestion[];
  intervalInMs: number;
  // value between 0 and 1
  // 0: no randomization, 1: randomization of 100%
  // Example: Interval (intervalInMs) is set to 60 minutes, randomization is set to 0.1
  // The experience sampling will be triggered between 54 and 66 minutes
  // After app startup or the last experience sampling
  samplingRandomization: number;
  allowUserToDisable?: boolean;
  allowUserToChangeInterval?: boolean;
  userDefinedInterval_h?: number[];
}

export type ExperienceSamplingAnswerType =
  | 'LikertScale'
  | 'TextResponse'
  | 'SingleChoice'
  | 'MultiChoice';

export interface ExperienceSamplingQuestionBase {
  question: string;
  answerType: ExperienceSamplingAnswerType;
}

export interface LikertScaleQuestion extends ExperienceSamplingQuestionBase {
  answerType: 'LikertScale';
  scale: number;
  responseOptions: string[];
}

export interface TextResponseQuestion extends ExperienceSamplingQuestionBase {
  answerType: 'TextResponse';
  responseOptions: 'singleLine' | 'multiLine';
  maxLength: number;
}

export interface ChoiceQuestion extends ExperienceSamplingQuestionBase {
  answerType: 'SingleChoice' | 'MultiChoice';
  responseOptions: string[];
}

export type ExperienceSamplingQuestion = LikertScaleQuestion | TextResponseQuestion | ChoiceQuestion;

export type DailySurveySamplingType = 'morning' | 'evening';

export interface DailySurveyConfig {
  samplingType: DailySurveySamplingType;
  delayInMinutes: number;
  questions: ExperienceSamplingQuestion[];
}

export interface DailySurveyTrackerConfiguration {
  enabled: boolean;
  requireAllAnswers?: boolean;
  surveys: DailySurveyConfig[];
}

export interface TrackerConfiguration {
  windowActivityTracker: WindowActivityTrackerConfiguration;
  userInputTracker: UserInputTrackerConfiguration;
  experienceSamplingTracker: ExperienceSamplingTrackerConfiguration;
  dailySurveyTracker?: DailySurveyTrackerConfiguration;
}

export interface StudyConfiguration {
  name: string;
  shortDescription: string;
  infoUrl: string;
  privacyPolicyUrl: string;
  uploadUrl: string;
  contactName: string;
  contactEmail: string;
  subjectIdLength: number;
  dataExportEnabled: boolean;
  dataExportEncrypted: boolean;
  dataExportFormat: DataExportFormat;
  dataExportDDLProjectName?: string;
  trackers: TrackerConfiguration;
  displayDaysParticipated: boolean;
  showActiveTimesInOnboarding?: boolean;
  enableRetrospection?: boolean;
}
