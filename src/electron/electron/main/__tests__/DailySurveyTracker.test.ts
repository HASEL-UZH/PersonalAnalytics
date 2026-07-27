import { afterEach, beforeEach, expect, jest, test } from '@jest/globals';
import type { DailySurveyConfig } from '../../../shared/StudyConfiguration';

const findOneByMock = jest.fn();
const scheduleJobMock = jest.fn();
const createDailySurveyWindowMock = jest.fn();
const closeDailySurveyWindowMock = jest.fn();
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

const morningSurvey: DailySurveyConfig = {
  samplingType: 'morning',
  delayInMinutes: 0,
  requireAllAnswers: false,
  questions: []
};

function createSettings(overrides: Record<string, unknown> = {}) {
  return {
    nextDailySurveyMorningInvocation: null,
    nextDailySurveyEveningInvocation: null,
    pendingDailySurveyMorningScheduledDate: null,
    pendingDailySurveyEveningScheduledDate: null,
    postponedDailySurveyMorningUntil: null,
    postponedDailySurveyEveningUntil: null,
    save: jest.fn(),
    ...overrides
  };
}

const workSchedule = {
  monday: { isWorking: true, startTime: '09:00', endTime: '18:00' },
  tuesday: { isWorking: true, startTime: '09:00', endTime: '18:00' },
  wednesday: { isWorking: true, startTime: '09:00', endTime: '18:00' },
  thursday: { isWorking: true, startTime: '09:00', endTime: '18:00' },
  friday: { isWorking: true, startTime: '09:00', endTime: '18:00' },
  saturday: { isWorking: false, startTime: '09:00', endTime: '18:00' },
  sunday: { isWorking: false, startTime: '09:00', endTime: '18:00' }
};

function createTracker(surveys: DailySurveyConfig[] = [eveningSurvey]) {
  return new DailySurveyTracker(
    {
      createDailySurveyWindow: createDailySurveyWindowMock,
      closeDailySurveyWindow: closeDailySurveyWindowMock
    } as never,
    { getWorkSchedule: jest.fn().mockResolvedValue(workSchedule) } as never,
    surveys
  );
}

function localDate(day: number, hour: number, minute = 0): Date {
  return new Date(2026, 4, day, hour, minute, 0, 0);
}

beforeEach(() => {
  jest.useFakeTimers();
  jest.setSystemTime(localDate(14, 10));
  jest.clearAllMocks();
  scheduleJobMock.mockReturnValue({ cancel: jest.fn() });
});

afterEach(() => {
  jest.useRealTimers();
});

test('shows a missed survey up to three calendar days later and schedules the current workday', async () => {
  const originalScheduledDate = localDate(15, 18);
  const settings = createSettings({
    nextDailySurveyEveningInvocation: originalScheduledDate
  });

  findOneByMock.mockResolvedValue(settings);
  jest.setSystemTime(localDate(18, 10));

  const tracker = createTracker();
  await tracker.start();

  expect(createDailySurveyWindowMock).toHaveBeenCalledWith('evening', originalScheduledDate);
  expect(settings.pendingDailySurveyEveningScheduledDate).toEqual(originalScheduledDate);
  expect(settings.nextDailySurveyEveningInvocation).toEqual(localDate(18, 18));
  expect(settings.save).toHaveBeenCalledTimes(1);
});

test('does not show a missed survey from four or more calendar days ago', async () => {
  const originalScheduledDate = localDate(10, 18);
  const settings = createSettings({
    nextDailySurveyEveningInvocation: originalScheduledDate
  });
  findOneByMock.mockResolvedValue(settings);

  const tracker = createTracker();
  await tracker.start();

  expect(createDailySurveyWindowMock).not.toHaveBeenCalled();
  expect(settings.pendingDailySurveyEveningScheduledDate).toBeNull();
  expect(settings.nextDailySurveyEveningInvocation).toEqual(localDate(14, 18));
  expect(settings.save).toHaveBeenCalledTimes(1);
});

test('clears a persisted missed survey after four calendar days', async () => {
  const originalScheduledDate = localDate(10, 18);
  const settings = createSettings({
    pendingDailySurveyEveningScheduledDate: originalScheduledDate,
    nextDailySurveyEveningInvocation: localDate(14, 18)
  });
  findOneByMock.mockResolvedValue(settings);

  const tracker = createTracker();
  await tracker.start();

  expect(createDailySurveyWindowMock).not.toHaveBeenCalled();
  expect(settings.pendingDailySurveyEveningScheduledDate).toBeNull();
  expect(settings.nextDailySurveyEveningInvocation).toEqual(localDate(14, 18));
  expect(settings.save).toHaveBeenCalledTimes(1);
});

test('shows the current survey after an older survey remains unanswered', async () => {
  const missedSurveyDate = localDate(13, 18);
  const settings = createSettings({
    nextDailySurveyEveningInvocation: missedSurveyDate
  });
  let dueCheck: (() => Promise<void>) | undefined;

  findOneByMock.mockResolvedValue(settings);
  scheduleJobMock.mockImplementation((_: string, callback: () => Promise<void>) => {
    dueCheck = callback;
    return { cancel: jest.fn() };
  });

  const tracker = createTracker();
  await tracker.start();

  jest.setSystemTime(localDate(14, 18, 1));
  await dueCheck?.();

  expect(createDailySurveyWindowMock).toHaveBeenNthCalledWith(1, 'evening', missedSurveyDate);
  expect(createDailySurveyWindowMock).toHaveBeenNthCalledWith(2, 'evening', localDate(14, 18));
  expect(settings.pendingDailySurveyEveningScheduledDate).toEqual(localDate(14, 18));
  expect(settings.nextDailySurveyEveningInvocation).toEqual(localDate(15, 18));
});

test('does not reopen an already shown survey when another survey type opens', async () => {
  const settings = createSettings({
    pendingDailySurveyMorningScheduledDate: localDate(14, 9),
    pendingDailySurveyEveningScheduledDate: localDate(13, 18),
    nextDailySurveyEveningInvocation: localDate(14, 18)
  });
  let dueCheck: (() => Promise<void>) | undefined;

  findOneByMock.mockResolvedValue(settings);
  scheduleJobMock.mockImplementation((_: string, callback: () => Promise<void>) => {
    dueCheck = callback;
    return { cancel: jest.fn() };
  });

  const tracker = createTracker([morningSurvey, eveningSurvey]);
  await tracker.start();
  await dueCheck?.();

  expect(createDailySurveyWindowMock).toHaveBeenCalledTimes(2);
  expect(createDailySurveyWindowMock).toHaveBeenNthCalledWith(1, 'morning', localDate(14, 9));
  expect(createDailySurveyWindowMock).toHaveBeenNthCalledWith(2, 'evening', localDate(13, 18));
});

test('completing an older survey leaves the current survey scheduled', async () => {
  const missedSurveyDate = localDate(13, 18);
  const currentSurveyDate = localDate(14, 18);
  const settings = createSettings({
    pendingDailySurveyEveningScheduledDate: missedSurveyDate,
    nextDailySurveyEveningInvocation: currentSurveyDate
  });
  findOneByMock.mockResolvedValue(settings);

  const tracker = createTracker();
  await tracker.complete('evening', missedSurveyDate);

  expect(settings.pendingDailySurveyEveningScheduledDate).toBeNull();
  expect(settings.nextDailySurveyEveningInvocation).toEqual(currentSurveyDate);
  expect(settings.save).toHaveBeenCalledTimes(1);
});

test('does not allow an overdue survey to be postponed', async () => {
  const originalScheduledDate = localDate(13, 18);
  const settings = createSettings({
    pendingDailySurveyEveningScheduledDate: originalScheduledDate,
    nextDailySurveyEveningInvocation: localDate(14, 18)
  });
  findOneByMock.mockResolvedValue(settings);

  const tracker = createTracker();

  await expect(tracker.postpone('evening', originalScheduledDate, 60)).resolves.toBe(false);
  expect(settings.postponedDailySurveyEveningUntil).toBeNull();
  expect(settings.save).not.toHaveBeenCalled();
});

test('stores a current-day postponement separately from its scheduled date', async () => {
  const originalScheduledDate = localDate(14, 8);
  const settings = createSettings({
    pendingDailySurveyEveningScheduledDate: originalScheduledDate,
    nextDailySurveyEveningInvocation: localDate(15, 18)
  });
  findOneByMock.mockResolvedValue(settings);

  const tracker = createTracker();

  await expect(tracker.postpone('evening', originalScheduledDate, 60)).resolves.toBe(true);
  expect(settings.pendingDailySurveyEveningScheduledDate).toEqual(originalScheduledDate);
  expect(settings.nextDailySurveyEveningInvocation).toEqual(localDate(15, 18));
  expect(settings.postponedDailySurveyEveningUntil).toEqual(localDate(14, 11));
  expect(settings.save).toHaveBeenCalledTimes(1);
});
