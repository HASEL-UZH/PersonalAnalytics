import { expect, test } from '@jest/globals';
import { getRetrospectionTrackerAvailability } from '../availability';

test('all retrospection sections are available when all required trackers are enabled', () => {
  expect(getRetrospectionTrackerAvailability(true, true, true)).toEqual({
    activityInsightsEnabled: true,
    selfReportsEnabled: true,
    messages: []
  });
});

test('window activity tracking disables activity insights without hiding self-reports', () => {
  const availability = getRetrospectionTrackerAvailability(false, true, true);

  expect(availability.activityInsightsEnabled).toBe(false);
  expect(availability.selfReportsEnabled).toBe(true);
  expect(availability.messages).toContain(
    'Window activity tracking is disabled, so activity insights are unavailable.'
  );
});

test('user-input tracking is required for current active-time-based activity insights', () => {
  const availability = getRetrospectionTrackerAvailability(true, false, true);

  expect(availability.activityInsightsEnabled).toBe(false);
  expect(availability.selfReportsEnabled).toBe(true);
  expect(availability.messages).toContain(
    'User-input tracking is disabled, so active-time-based activity insights are unavailable.'
  );
});

test('experience sampling can be disabled without hiding activity insights', () => {
  const availability = getRetrospectionTrackerAvailability(true, true, false);

  expect(availability.activityInsightsEnabled).toBe(true);
  expect(availability.selfReportsEnabled).toBe(false);
  expect(availability.messages).toContain(
    'Experience sampling is disabled, so self-reports are unavailable.'
  );
});

test('all retrospection sections are unavailable when all data trackers are disabled', () => {
  const availability = getRetrospectionTrackerAvailability(false, false, false);

  expect(availability.activityInsightsEnabled).toBe(false);
  expect(availability.selfReportsEnabled).toBe(false);
  expect(availability.messages).toHaveLength(2);
});
