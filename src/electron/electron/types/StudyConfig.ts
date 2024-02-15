export interface UserInputTrackerConfig {
  enabled: boolean;
}

export interface WindowActivityTrackerConfig {
  enabled: boolean;
  checkingForWindowChangeIntervalInMs: number;
}

export interface ExperienceSamplingConfig {
  enabled: boolean;
  question: string;
  responseOptions: string[];
  samplingIntervalInMinutes: number;
  samplingRandomization: boolean;
}

export interface TrackerConfig {
  windowActivityTracker: WindowActivityTrackerConfig;
  userInputTracker: UserInputTrackerConfig;
  experienceSampling: ExperienceSamplingConfig;
}

export interface StudyConfig {
  name: string;
  shortDescription: string;
  infoUrl: string;
  privacyPolicyUrl: string;
  uploadUrl: string;
  contactName: string;
  contactEmail: string;
  subjectIdLength: number;
  trackers: TrackerConfig;
}
