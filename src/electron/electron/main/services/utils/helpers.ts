import electron from 'electron';

export declare interface Is {
  dev: boolean;
  macOS: boolean;
  windows: boolean;
}

export const is: Is = {
  dev: !electron.app.isPackaged,
  macOS: process.platform === 'darwin',
  windows: process.platform === 'win32'
};

export function generateAlphaNumericString(length: number = 0): string {
  if (length <= 0) {
    throw new Error('Length must be greater than 0');
  }

  const characters = 'ABCDEFGHJKLMNPQRSTUVWXYZ123456789'; // removed 0, O, I and l from options to avoid participant IDs that are ambiguous
  let result = '';
  for (let i = 0; i < length; i++) {
    result += characters.charAt(Math.floor(Math.random() * characters.length));
  }
  return result;
}

/**
 * Formats a date as a SQLite-local datetime string.
 *
 * Queries that compare against `datetime(..., 'localtime')` need local wall-clock parameters
 * instead of UTC ISO strings.
 */
export function formatSqliteLocalDateTime(date: Date): string {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  const hours = `${date.getHours()}`.padStart(2, '0');
  const minutes = `${date.getMinutes()}`.padStart(2, '0');
  const seconds = `${date.getSeconds()}`.padStart(2, '0');
  return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
}

export function getOverlapDurationMs(
  firstStart: Date,
  firstEnd: Date,
  secondStart: Date,
  secondEnd: Date
): number {
  return Math.max(
    0,
    Math.min(firstEnd.getTime(), secondEnd.getTime()) -
      Math.max(firstStart.getTime(), secondStart.getTime())
  );
}
