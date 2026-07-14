export interface RetrospectionTrackerAvailability {
  activityInsightsEnabled: boolean;
  selfReportsEnabled: boolean;
  messages: string[];
}

export function getRetrospectionTrackerAvailability(
  windowActivityEnabled: boolean,
  userInputEnabled: boolean,
  experienceSamplingEnabled: boolean
): RetrospectionTrackerAvailability {
  const messages: string[] = [];
  if (!windowActivityEnabled) {
    messages.push('Window activity tracking is disabled, so activity insights are unavailable.');
  } else if (!userInputEnabled) {
    messages.push(
      'User-input tracking is disabled, so active-time-based activity insights are unavailable.'
    );
  }
  if (!experienceSamplingEnabled) {
    messages.push('Experience sampling is disabled, so self-reports are unavailable.');
  }

  return {
    activityInsightsEnabled: windowActivityEnabled && userInputEnabled,
    selfReportsEnabled: experienceSamplingEnabled,
    messages
  };
}
