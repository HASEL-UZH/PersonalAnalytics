import { afterEach, beforeEach, expect, jest, test } from '@jest/globals';
import type { DailySurveyConfig } from '../../../shared/StudyConfiguration';

const findOneByMock = jest.fn();
const scheduleJobMock = jest.fn();
const createDailySurveyWindowMock = jest.fn();
const logInfoMock = jest.fn();
const logErrorMock = jest.fn();

jest.unstable_mockModule('node-schedule', () => ({
  scheduleJob: scheduleJobMock
}));

jest.unstable_mockModule('../entities/Settings', () => ({
  Settings: {
    findOneBy: findOneByMock
  }
}));

jest.unstable_mockModule('../services/WindowService', () => ({
  WindowService: class WindowService {}
}));

jest.unstable_mockModule('../services/WorkScheduleService', () => ({
  WorkScheduleService: class WorkScheduleService {}
}));

jest.unstable_mockModule('../../config/Logger', () => ({
  default: () => ({
    info: logInfoMock,
    error: logErrorMock
  })
}));

const { DailySurveyTracker } = await import('../services/trackers/DailySurveyTracker');

const eveningSurvey: DailySurveyConfig = {
  samplingType: 'evening',
  delayInMinutes: 0,
  requireAllAnswers: false,
  questions: []
};

function createSettings(overrides: Record<string, unknown> = {}) {
  return {
    nextDailySurveyMorningInvocation: null,
    nextDailySurveyEveningInvocation: null,
    postponedDailySurveyMorningUntil: null,
    postponedDailySurveyEveningUntil: null,
    save: jest.fn(),
    ...overrides
  };
}

function createTracker() {
  return new DailySurveyTracker(
    { createDailySurveyWindow: createDailySurveyWindowMock } as never,
    { getWorkSchedule: jest.fn() } as never,
    [eveningSurvey]
  );
}

beforeEach(() => {
  jest.useFakeTimers();
  jest.setSystemTime(new Date('2026-05-14T10:00:00.000Z'));
  jest.clearAllMocks();
  scheduleJobMock.mockReturnValue({ cancel: jest.fn() });
});

afterEach(() => {
  jest.useRealTimers();
});

test('preserves an overdue survey date when the computer resumes on a later day', async () => {
  const originalScheduledDate = new Date('2026-05-13T16:10:00.000Z');
  const settings = createSettings({
    nextDailySurveyEveningInvocation: originalScheduledDate
  });
  let dueCheck: (() => Promise<void>) | undefined;

  findOneByMock.mockResolvedValue(settings);
  scheduleJobMock.mockImplementation((_: string, callback: () => Promise<void>) => {
    dueCheck = callback;
    return { cancel: jest.fn() };
  });

  const tracker = createTracker();
  await tracker.resume();
  await dueCheck?.();

  expect(createDailySurveyWindowMock).toHaveBeenCalledWith('evening', originalScheduledDate);
  expect(settings.nextDailySurveyEveningInvocation).toBe(originalScheduledDate);
  expect(settings.save).not.toHaveBeenCalled();
});

test('preserves an overdue survey date when the application starts on a later day', async () => {
  const originalScheduledDate = new Date('2026-05-13T16:10:00.000Z');
  const settings = createSettings({
    nextDailySurveyEveningInvocation: originalScheduledDate
  });
  findOneByMock.mockResolvedValue(settings);

  const tracker = createTracker();
  await tracker.start();

  expect(createDailySurveyWindowMock).toHaveBeenCalledWith('evening', originalScheduledDate);
  expect(settings.nextDailySurveyEveningInvocation).toBe(originalScheduledDate);
  expect(settings.save).not.toHaveBeenCalled();
});

test('does not allow an overdue survey to be postponed', async () => {
  const settings = createSettings({
    nextDailySurveyEveningInvocation: new Date('2026-05-13T16:10:00.000Z')
  });
  findOneByMock.mockResolvedValue(settings);

  const tracker = createTracker();

  await expect(tracker.postpone('evening', 60)).resolves.toBe(false);
  expect(settings.postponedDailySurveyEveningUntil).toBeNull();
  expect(settings.save).not.toHaveBeenCalled();
});

test('stores a current-day postponement separately from its scheduled date', async () => {
  const originalScheduledDate = new Date('2026-05-14T08:00:00.000Z');
  const settings = createSettings({
    nextDailySurveyEveningInvocation: originalScheduledDate
  });
  findOneByMock.mockResolvedValue(settings);

  const tracker = createTracker();

  await expect(tracker.postpone('evening', 60)).resolves.toBe(true);
  expect(settings.nextDailySurveyEveningInvocation).toBe(originalScheduledDate);
  expect(settings.postponedDailySurveyEveningUntil).toEqual(
    new Date('2026-05-14T11:00:00.000Z')
  );
  expect(settings.save).toHaveBeenCalledTimes(1);
});
