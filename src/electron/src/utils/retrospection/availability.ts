export interface RetrospectionTrackerAvailability {
  activityInsightsEnabled: boolean;
  selfReportsEnabled: boolean;
  activityInsightMessages: string[];
  selfReportMessages: string[];
  messages: string[];
}

interface TrackerAvailabilityConfig {
  name: string;
  enabled: boolean;
}

export function getRetrospectionTrackerAvailability(
  windowActivityMonitor: TrackerAvailabilityConfig,
  userInputMonitor: TrackerAvailabilityConfig,
  experienceSampling: TrackerAvailabilityConfig
): RetrospectionTrackerAvailability {
  const activityInsightMessages: string[] = [];
  const selfReportMessages: string[] = [];

  if (!windowActivityMonitor.enabled) {
    activityInsightMessages.push(
      `${windowActivityMonitor.name} is disabled, so no activity insights can be visualized.`
    );
  }
  if (!userInputMonitor.enabled) {
    activityInsightMessages.push(
      `${userInputMonitor.name} is disabled, so no activity insights can be visualized.`
    );
  }
  if (!experienceSampling.enabled) {
    selfReportMessages.push(
      `${experienceSampling.name} is disabled, so no self-reports can be visualized.`
    );
  }

  return {
    activityInsightsEnabled: windowActivityMonitor.enabled && userInputMonitor.enabled,
    selfReportsEnabled: experienceSampling.enabled,
    activityInsightMessages,
    selfReportMessages,
    messages: [...activityInsightMessages, ...selfReportMessages]
  };
}
