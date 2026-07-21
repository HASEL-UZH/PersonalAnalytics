export const RETROSPECTION_WORKDAY_CUTOFF_HOUR = 4;

export interface RetrospectionWorkdayRange {
  start: Date;
  end: Date;
}

export interface RetrospectionTimelineBounds {
  start: number;
  end: number;
}

function toLocalDate(date: Date | string): Date {
  if (date instanceof Date) return date;
  const parts = /^(\d{4})-(\d{2})-(\d{2})$/.exec(date);
  return parts
    ? new Date(Number(parts[1]), Number(parts[2]) - 1, Number(parts[3]))
    : new Date(date);
}

/** Returns the selected local calendar day's 04:00-to-04:00 workday. */
export function getRetrospectionWorkdayRange(date: Date | string): RetrospectionWorkdayRange {
  const d = toLocalDate(date);
  const start = new Date(
    d.getFullYear(),
    d.getMonth(),
    d.getDate(),
    RETROSPECTION_WORKDAY_CUTOFF_HOUR,
    0,
    0,
    0
  );
  const end = new Date(start);
  end.setDate(end.getDate() + 1);
  return { start, end };
}

export function getWorkdayMinuteIndex(date: Date, workdayStart: Date): number {
  return Math.floor((date.getTime() - workdayStart.getTime()) / 60000);
}

export function getDateFromWorkdayMinuteIndex(minuteIndex: number, workdayStart: Date): Date {
  return new Date(workdayStart.getTime() + minuteIndex * 60000);
}

/** Formats a local timestamp for SQLite `datetime(..., 'localtime')` comparison. */
export function formatSqliteLocalDateTime(date: Date): string {
  const parts = [
    date.getFullYear(),
    `${date.getMonth() + 1}`.padStart(2, '0'),
    `${date.getDate()}`.padStart(2, '0')
  ];
  const time = [date.getHours(), date.getMinutes(), date.getSeconds()]
    .map((value) => `${value}`.padStart(2, '0'))
    .join(':');
  return `${parts.join('-')} ${time}`;
}

function floorToLocalHour(timestamp: number): number {
  const date = new Date(timestamp);
  date.setMinutes(0, 0, 0);
  return date.getTime();
}

/** Computes one hour-aligned x-axis domain for every retrospection timeline. */
export function getRetrospectionTimelineBounds(
  selectedDay: Date | string,
  timestamps: Array<Date | number | string | null | undefined>
): RetrospectionTimelineBounds | null {
  const values = timestamps
    .map((timestamp) =>
      timestamp instanceof Date
        ? timestamp.getTime()
        : typeof timestamp === 'string'
          ? new Date(timestamp).getTime()
          : timestamp
    )
    .filter((timestamp): timestamp is number => typeof timestamp === 'number' && Number.isFinite(timestamp));
  const { start: workdayStartDate, end: workdayEndDate } = getRetrospectionWorkdayRange(selectedDay);
  const workdayStart = workdayStartDate.getTime();
  const workdayEnd = workdayEndDate.getTime();
  const inRange = values.filter((timestamp) => timestamp >= workdayStart && timestamp < workdayEnd);
  if (!inRange.length) return null;

  const start = Math.max(floorToLocalHour(Math.min(...inRange)), workdayStart);
  let end = Math.min(floorToLocalHour(Math.max(...inRange)) + 60 * 60 * 1000, workdayEnd);
  if (end <= start) end = Math.min(start + 60 * 60 * 1000, workdayEnd);
  return { start, end };
}
