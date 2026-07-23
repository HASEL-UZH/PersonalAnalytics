/** Builds the active-hours total and longest uninterrupted active period figures. */
import type { ActiveHoursInsight, TimeActive } from '../../../../../src/utils/retrospection/types';
import { getDateFromWorkdayMinuteIndex } from '../../../../../shared/retrospection/Workday';
import type { RetrospectionWorkdayData } from '../RetrospectionWorkdayData';

export function buildActiveHoursFigure(workdayData: RetrospectionWorkdayData): ActiveHoursInsight {
  return { activeDurationMs: workdayData.activeMinutes.size * 60_000 };
}

export function buildLongestActivePeriodFigure(workdayData: RetrospectionWorkdayData): TimeActive {
  let longest: TimeActive = { from: new Date(), to: new Date(), duration: -1 };
  let periodStart: number | undefined;

  for (let minute = 0; minute < 24 * 60; minute++) {
    if (workdayData.activeMinutes.has(minute) && periodStart === undefined) {
      periodStart = minute;
    } else if (!workdayData.activeMinutes.has(minute) && periodStart !== undefined) {
      const duration = minute - periodStart;
      if (duration > longest.duration) {
        longest = {
          from: getDateFromWorkdayMinuteIndex(periodStart, workdayData.workdayStart),
          to: getDateFromWorkdayMinuteIndex(minute, workdayData.workdayStart),
          duration
        };
      }
      periodStart = undefined;
    }
  }

  if (periodStart !== undefined) {
    const duration = 24 * 60 - periodStart;
    if (duration > longest.duration) {
      longest = {
        from: getDateFromWorkdayMinuteIndex(periodStart, workdayData.workdayStart),
        to: getDateFromWorkdayMinuteIndex(24 * 60, workdayData.workdayStart),
        duration
      };
    }
  }

  return longest;
}
