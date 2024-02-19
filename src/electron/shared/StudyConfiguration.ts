export interface UserInputTrackerConfiguration {
  enabled: boolean;
  intervalInMs: number;
}

export interface WindowActivityTrackerConfiguration {
  enabled: boolean;
  intervalInMs: number;
}

export interface ExperienceSamplingConfiguration {
  enabled: boolean;
  scale: number;
  questions: string[];
  responseOptions: string[][];
  samplingIntervalInMinutes: number;
  samplingRandomization: boolean;
}

export interface TrackerConfiguration {
  windowActivityTracker: WindowActivityTrackerConfiguration;
  userInputTracker: UserInputTrackerConfiguration;
  experienceSampling: ExperienceSamplingConfiguration;
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
  trackers: TrackerConfiguration;
}
