import { UserInputEntity } from '../../entities/UserInputEntity';
import { WindowActivityEntity } from '../../entities/WindowActivityEntity';
import {
  formatSqliteLocalDateTime,
  getRetrospectionWorkdayRange,
  getWorkdayMinuteIndex
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

export interface RetrospectionSnapshot {
  activeMinutes: Set<number>;
  windowActivities: WindowActivityEntity[];
  workdayStart: Date;
}

export async function getActiveMinutesForWorkday(date: Date | string): Promise<Set<number>> {
  const { start, end } = getRetrospectionWorkdayRange(date);
  const workdayStart = formatSqliteLocalDateTime(start);
  const workdayEnd = formatSqliteLocalDateTime(end);
  const userInput = await UserInputEntity.createQueryBuilder('userInput')
    .select(['userInput.*', "datetime(userInput.tsStart, 'localtime') as tsStart"])
    .where("datetime(userInput.tsStart, 'localtime') >= :workdayStart", { workdayStart })
    .andWhere("datetime(userInput.tsStart, 'localtime') < :workdayEnd", { workdayEnd })
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
      const minuteIndex = getWorkdayMinuteIndex(new Date(entry.tsStart), start);
      if (minuteIndex >= 0 && minuteIndex < 24 * 60) {
        activeMinutes.add(minuteIndex);
      }
    }
  });

  return activeMinutes;
}

export async function getWindowActivitiesForWorkday(
  date: Date | string
): Promise<WindowActivityEntity[]> {
  const { start, end } = getRetrospectionWorkdayRange(date);
  const workdayStart = formatSqliteLocalDateTime(start);
  const workdayEnd = formatSqliteLocalDateTime(end);
  const rows = await WindowActivityEntity.createQueryBuilder('windowActivity')
    .select(['windowActivity.*', "datetime(windowActivity.ts, 'localtime') as ts"])
    .where("datetime(windowActivity.ts, 'localtime') >= :workdayStart", { workdayStart })
    .andWhere("datetime(windowActivity.ts, 'localtime') < :workdayEnd", { workdayEnd })
    .orderBy('windowActivity.ts', 'ASC')
    .getRawMany<RawWindowActivity>();

  return rows.map((row) => ({ ...row, ts: new Date(row.ts) }) as WindowActivityEntity);
}

/** Loads the raw tracker data once for every figure on the selected workday. */
export async function loadRetrospectionSnapshot(
  date: Date | string
): Promise<RetrospectionSnapshot> {
  const { start } = getRetrospectionWorkdayRange(date);
  const [windowActivities, activeMinutes] = await Promise.all([
    getWindowActivitiesForWorkday(date),
    getActiveMinutesForWorkday(date)
  ]);

  return { activeMinutes, windowActivities, workdayStart: start };
}
