/**
 * Loads the raw Window Activity Monitor and User Input Monitor records for one retrospection
 * workday. Figure modules share this data so a dashboard load queries each source only once.
 */
import { UserInputEntity } from '../../entities/UserInputEntity';
import { WindowActivityEntity } from '../../entities/WindowActivityEntity';
import {
  formatSqliteUtcDateTime,
  getWorkdayMinuteIndex,
  parseSqliteUtcDateTime,
  type RetrospectionWorkdayRange
} from '../../../../shared/retrospection/Workday';

interface RawUserInput {
  clickTotal?: number;
  keysTotal?: number;
  movedDistance?: number;
  scrollDelta?: number;
  tsStart: Date | string;
}

interface RawWindowActivity extends Omit<WindowActivityEntity, 'ts'> {
  ts: Date | string;
}

/** Raw tracker data and derived active-minute indexes shared by retrospection figures. */
export interface RetrospectionWorkdayData {
  activeMinutes: Set<number>;
  windowActivities: WindowActivityEntity[];
  workdayStart: Date;
}

export async function getActiveMinutesForWorkday(workdayRange: RetrospectionWorkdayRange): Promise<Set<number>> {
  const workdayStart = formatSqliteUtcDateTime(workdayRange.start);
  const workdayEnd = formatSqliteUtcDateTime(workdayRange.end);
  const workdayMinuteCount = Math.round(
    (workdayRange.end.getTime() - workdayRange.start.getTime()) / 60_000
  );
  const userInput = await UserInputEntity.createQueryBuilder('userInput')
    .select(['userInput.*'])
    .where('userInput.tsStart >= :workdayStart', { workdayStart })
    .andWhere('userInput.tsStart < :workdayEnd', { workdayEnd })
    .orderBy('userInput.tsStart', 'ASC')
    .getRawMany<RawUserInput>();

  const activeMinutes = new Set<number>();
  userInput.forEach((entry) => {
    if (
      (entry.clickTotal ?? 0) > 0 ||
      (entry.keysTotal ?? 0) > 0 ||
      (entry.scrollDelta ?? 0) > 0 ||
      (entry.movedDistance ?? 0) > 0
    ) {
      const minuteIndex = getWorkdayMinuteIndex(parseSqliteUtcDateTime(entry.tsStart), workdayRange.start);
      if (minuteIndex >= 0 && minuteIndex < workdayMinuteCount) {
        activeMinutes.add(minuteIndex);
      }
    }
  });

  return activeMinutes;
}

export async function getWindowActivitiesForWorkday(
  workdayRange: RetrospectionWorkdayRange
): Promise<WindowActivityEntity[]> {
  const workdayStart = formatSqliteUtcDateTime(workdayRange.start);
  const workdayEnd = formatSqliteUtcDateTime(workdayRange.end);
  const rows = await WindowActivityEntity.createQueryBuilder('windowActivity')
    .select(['windowActivity.*'])
    .where('windowActivity.ts >= :workdayStart', { workdayStart })
    .andWhere('windowActivity.ts < :workdayEnd', { workdayEnd })
    .orderBy('windowActivity.ts', 'ASC')
    .getRawMany<RawWindowActivity>();

  return rows.map((row) => ({ ...row, ts: parseSqliteUtcDateTime(row.ts) }) as WindowActivityEntity);
}

/** Loads the shared raw data once for every figure on the selected workday. */
export async function loadRetrospectionWorkdayData(
  workdayRange: RetrospectionWorkdayRange
): Promise<RetrospectionWorkdayData> {
  const [windowActivities, activeMinutes] = await Promise.all([
    getWindowActivitiesForWorkday(workdayRange),
    getActiveMinutesForWorkday(workdayRange)
  ]);

  return { activeMinutes, windowActivities, workdayStart: workdayRange.start };
}
