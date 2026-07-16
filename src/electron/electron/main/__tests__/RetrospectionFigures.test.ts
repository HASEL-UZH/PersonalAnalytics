import { expect, test } from '@jest/globals';
import { jest } from '@jest/globals';
import type { WindowActivityEntity } from '../entities/WindowActivityEntity';
import type { RetrospectionWorkdayData } from '../services/retrospection/RetrospectionWorkdayData';

const mockIcon = {
  isEmpty: () => false,
  resize: () => ({ toDataURL: () => 'data:image/png;base64,mock-icon' })
};
jest.unstable_mockModule('electron', () => ({
  app: { getFileIcon: jest.fn(async () => mockIcon), isPackaged: false },
  nativeImage: { createFromPath: jest.fn(() => mockIcon) },
  default: { app: { isPackaged: false } }
}));

const { buildActiveHoursFigure, buildLongestActivePeriodFigure } =
  await import('../services/retrospection/figures/ActiveHoursFigure');
const { buildTopAppsFigure } = await import('../services/retrospection/figures/TopAppsFigure');
const { buildTopWebsitesFigure } =
  await import('../services/retrospection/figures/TopWebsitesFigure');
const { buildTopWindowTitlesFigure } =
  await import('../services/retrospection/figures/TopWindowTitlesFigure');

const workdayStart = new Date(2026, 6, 14, 4);
const activeMinutes = new Set(Array.from({ length: 62 }, (_, index) => 5 * 60 + index));
const windowActivities = [
  {
    activity: 'DevCode',
    processName: 'Code',
    processPath: '/mock/Code',
    ts: new Date(2026, 6, 14, 9),
    url: null,
    windowTitle: 'main.ts - Code'
  },
  {
    activity: 'WorkRelatedBrowsing',
    processName: 'Microsoft Edge',
    processPath: '/mock/Microsoft Edge',
    ts: new Date(2026, 6, 14, 10),
    url: 'https://github.com/HASEL-UZH/PersonalAnalytics/pull/540',
    windowTitle: 'Pull request review - GitHub - Microsoft Edge'
  }
] as WindowActivityEntity[];
const workdayData: RetrospectionWorkdayData = { activeMinutes, windowActivities, workdayStart };

test('active-hours figures derive their values from the shared workday data', () => {
  expect(buildActiveHoursFigure(workdayData)).toEqual({ activeDurationMs: 62 * 60_000 });
  expect(buildLongestActivePeriodFigure(workdayData)).toEqual({
    from: new Date(2026, 6, 14, 9),
    to: new Date(2026, 6, 14, 10, 2),
    duration: 62
  });
});

test('top figures independently transform and decorate the same raw workday data', async () => {
  const topApps = await buildTopAppsFigure(workdayData);
  expect(
    topApps.map(({ type, totalDurationMs }) => ({
      type,
      totalDurationMs
    }))
  ).toEqual([
    { type: 'Code', totalDurationMs: 60 * 60_000 },
    { type: 'Microsoft Edge', totalDurationMs: 60_000 }
  ]);
  expect(topApps.every((item) => item.iconDataUrl === 'data:image/png;base64,mock-icon')).toBe(
    true
  );
  expect(await buildTopWebsitesFigure(workdayData)).toEqual([
    expect.objectContaining({
      type: 'Pull request review - GitHub',
      totalDurationMs: 60_000,
      iconDataUrl: 'data:image/png;base64,mock-icon'
    })
  ]);
  expect(await buildTopWindowTitlesFigure(workdayData)).toEqual([
    expect.objectContaining({
      type: 'main.ts',
      totalDurationMs: 60 * 60_000,
      iconDataUrl: 'data:image/png;base64,mock-icon'
    })
  ]);
});
