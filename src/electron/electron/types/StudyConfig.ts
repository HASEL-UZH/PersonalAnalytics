export interface UserInputTrackerConfig {
  enabled: boolean;
}

export interface WindowActivityTrackerConfig {
  enabled: boolean;
}

export interface ExperienceSamplingConfig {
  enabled: boolean;
  question: string;
  responseOptions: string[];
  samplingIntervalInMinutes: number;
  samplingRandomization: boolean;
}

export interface StudyConfig {
  name: string;
  shortDescription: string;
  infoUrl: string;
  privacyPolicyUrl: string;
  uploadUrl: string;
  contactName: string;
  contactEmail: string;
  trackers: {
    windowActivityTracker: WindowActivityTrackerConfig;
    userInputTracker: UserInputTrackerConfig;
    experienceSampling: ExperienceSamplingConfig;
  };
}
