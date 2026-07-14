import type { ActiveHoursInsight, TimeActive } from '../../../../../src/utils/retrospection/types';
import { getDateFromWorkdayMinuteIndex } from '../../../../../shared/retrospection/Workday';
import type { RetrospectionSnapshot } from '../RetrospectionSnapshot';

export function buildActiveHoursFigure(snapshot: RetrospectionSnapshot): ActiveHoursInsight {
  return { activeDurationMs: snapshot.activeMinutes.size * 60_000 };
}

export function buildLongestActivePeriodFigure(snapshot: RetrospectionSnapshot): TimeActive {
  let longest: TimeActive = { from: new Date(), to: new Date(), duration: -1 };
  let periodStart: number | undefined;

  for (let minute = 0; minute < 24 * 60; minute++) {
    if (snapshot.activeMinutes.has(minute) && periodStart === undefined) {
      periodStart = minute;
    } else if (!snapshot.activeMinutes.has(minute) && periodStart !== undefined) {
      const duration = minute - periodStart;
      if (duration > longest.duration) {
        longest = {
          from: getDateFromWorkdayMinuteIndex(periodStart, snapshot.workdayStart),
          to: getDateFromWorkdayMinuteIndex(minute, snapshot.workdayStart),
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
        from: getDateFromWorkdayMinuteIndex(periodStart, snapshot.workdayStart),
        to: getDateFromWorkdayMinuteIndex(24 * 60, snapshot.workdayStart),
        duration
      };
    }
  }

  return longest;
}
