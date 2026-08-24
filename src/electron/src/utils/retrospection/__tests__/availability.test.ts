import { expect, test } from '@jest/globals';
import { getRetrospectionTrackerAvailability } from '../availability';

const windowActivityMonitor = { name: 'Window Activity Monitor', enabled: true };
const userInputMonitor = { name: 'User Input Monitor', enabled: true };
const experienceSampling = { name: 'Experience Sampling', enabled: true };

test('all retrospection sections are available when all required trackers are enabled', () => {
  expect(
    getRetrospectionTrackerAvailability(windowActivityMonitor, userInputMonitor, experienceSampling)
  ).toEqual({
    activityInsightsEnabled: true,
    selfReportsEnabled: true,
    activityInsightMessages: [],
    selfReportMessages: [],
    messages: []
  });
});

test('Window Activity Monitor disables activity insights without hiding self-reports', () => {
  const availability = getRetrospectionTrackerAvailability(
    { ...windowActivityMonitor, enabled: false },
    userInputMonitor,
    experienceSampling
  );

  expect(availability.activityInsightsEnabled).toBe(false);
  expect(availability.selfReportsEnabled).toBe(true);
  expect(availability.messages).toContain(
    'Window Activity Monitor is disabled, so no activity insights can be visualized.'
  );
});

test('User Input Monitor is required for current active-time-based activity insights', () => {
  const availability = getRetrospectionTrackerAvailability(
    windowActivityMonitor,
    { ...userInputMonitor, enabled: false },
    experienceSampling
  );

  expect(availability.activityInsightsEnabled).toBe(false);
  expect(availability.selfReportsEnabled).toBe(true);
  expect(availability.messages).toContain(
    'User Input Monitor is disabled, so no activity insights can be visualized.'
  );
});

test('experience sampling can be disabled without hiding activity insights', () => {
  const availability = getRetrospectionTrackerAvailability(
    windowActivityMonitor,
    userInputMonitor,
    { ...experienceSampling, enabled: false }
  );

  expect(availability.activityInsightsEnabled).toBe(true);
  expect(availability.selfReportsEnabled).toBe(false);
  expect(availability.messages).toContain(
    'Experience Sampling is disabled, so no self-reports can be visualized.'
  );
});

test('all disabled monitors are named in the retrospection messages', () => {
  const availability = getRetrospectionTrackerAvailability(
    { ...windowActivityMonitor, enabled: false },
    { ...userInputMonitor, enabled: false },
    { ...experienceSampling, enabled: false }
  );

  expect(availability.activityInsightsEnabled).toBe(false);
  expect(availability.selfReportsEnabled).toBe(false);
  expect(availability.messages).toHaveLength(3);
  expect(availability.messages.join(' ')).toContain('Window Activity Monitor');
  expect(availability.messages.join(' ')).toContain('User Input Monitor');
  expect(availability.messages.join(' ')).toContain('Experience Sampling');
});
