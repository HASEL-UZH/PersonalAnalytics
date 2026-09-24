import { beforeEach, expect, jest, test } from '@jest/globals';
import type { WorkHoursDto } from '../../../shared/dto/WorkHoursDto';

const cancelMock = jest.fn();
const scheduleJobMock = jest.fn(() => ({
  cancel: cancelMock,
  nextInvocation: () => new Date('2026-01-01T00:00:00Z')
}));

jest.unstable_mockModule('node-schedule', () => ({
  scheduleJob: scheduleJobMock
}));

jest.unstable_mockModule('../entities/Settings', () => ({
  Settings: { findOne: jest.fn() }
}));

jest.unstable_mockModule('../../../shared/study.config', () => ({
  default: { enableRetrospection: true }
}));

jest.unstable_mockModule('../../config/Logger', () => ({
  getMainLogger: () => ({ info: jest.fn(), debug: jest.fn(), error: jest.fn(), warn: jest.fn() }),
  default: () => ({ info: jest.fn(), debug: jest.fn(), error: jest.fn(), warn: jest.fn() })
}));

const { SchedulingService } = await import('../services/SchedulingService');

// Only Monday is a working day, so exactly one retrospection job (+ the 4am
// cleanup job) is created per (re)schedule.
const workSchedule: WorkHoursDto = {
  monday: { isWorking: true, startTime: '09:00', endTime: '18:00' },
  tuesday: { isWorking: false, startTime: '09:00', endTime: '18:00' },
  wednesday: { isWorking: false, startTime: '09:00', endTime: '18:00' },
  thursday: { isWorking: false, startTime: '09:00', endTime: '18:00' },
  friday: { isWorking: false, startTime: '09:00', endTime: '18:00' },
  saturday: { isWorking: false, startTime: '09:00', endTime: '18:00' },
  sunday: { isWorking: false, startTime: '09:00', endTime: '18:00' }
};

const windowService = {
  closeRetrospectionWindow: jest.fn(),
  focusOrCreateRetrospectionWindow: jest.fn()
};

beforeEach(() => {
  scheduleJobMock.mockClear();
  cancelMock.mockClear();
});

test('resume() cancels the previous jobs and recreates them', () => {
  const service = new SchedulingService(windowService as never, workSchedule);

  // 1 retrospection job (Monday) + 1 cleanup job created on construction.
  expect(scheduleJobMock).toHaveBeenCalledTimes(2);
  expect(cancelMock).not.toHaveBeenCalled();

  service.resume();

  // The pre-sleep jobs must be torn down explicitly...
  expect(cancelMock).toHaveBeenCalledTimes(2);
  // ...and fresh node-schedule jobs created in their place, otherwise the
  // service would be left with dead node-schedule timers after the OS wakes
  // (node-schedule/node-schedule#13).
  expect(scheduleJobMock).toHaveBeenCalledTimes(4);
});

test('resume() is safe to call repeatedly, e.g. across several sleep/wake cycles', () => {
  const service = new SchedulingService(windowService as never, workSchedule);

  service.resume();
  service.resume();
  service.resume();

  // constructor (2) + 3 resumes (2 each) = 8
  expect(scheduleJobMock).toHaveBeenCalledTimes(8);
  expect(cancelMock).toHaveBeenCalledTimes(6);
});
