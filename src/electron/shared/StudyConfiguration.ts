export interface UserInputTrackerConfiguration {
  enabled: boolean;
  intervalInMs: number;
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
  scale: number;
  questions: string[];
  responseOptions: string[][];
  intervalInMs: number;
  // value between 0 and 1
  // 0: no randomization, 1: randomization of 100%
  // Example: Interval (intervalInMs) is set to 60 minutes, randomization is set to 0.1
  // The experience sampling will be triggered between 54 and 66 minutes
  // After app startup or the last experience sampling
  samplingRandomization: number;
}

export interface TrackerConfiguration {
  windowActivityTracker: WindowActivityTrackerConfiguration;
  userInputTracker: UserInputTrackerConfiguration;
  experienceSamplingTracker: ExperienceSamplingTrackerConfiguration;
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
  trackers: TrackerConfiguration;
  displayDaysParticipated: boolean;
}
