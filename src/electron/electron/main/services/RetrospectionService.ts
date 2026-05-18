import { UserInputEntity } from '../entities/UserInputEntity';
import { WindowActivityEntity } from '../entities/WindowActivityEntity';
import { getMainLogger } from '../../config/Logger';

const LOG = getMainLogger('RetrospectionService');

export interface TimeActive {
  from: Date;
  to: Date;
  duration: number;
}

export interface ActivitySessions {
  type: string;
  totalDurationMs: number;
  sessions: TimeActive[];
  activity?: string;
}

type WindowActivitySessionKeySelector = (activity: WindowActivityEntity) => string | null;

const BROWSER_PROCESS_NAMES = [
  'Google Chrome',
  'Microsoft Edge',
  'Mozilla Firefox',
  'Firefox',
  'Safari',
  'Arc',
  'Brave Browser',
  'Opera'
];

/**
 * converts a date object to a minute of the day (0-1439)
 * @param date - date object
 * @returns the minute of the day (0-1439)
 */
function getMinuteOfDay(date: Date): number {
  return date.getHours() * 60 + date.getMinutes();
}

/**
 * constructs a date object from a minute of the day (0-1439)
 * @param minuteOfDay - minute of the day (0-1439)
 * @returns a date object
 */
function getDateFromMinuteOfDay(minuteOfDay: number, baseDate?: Date): Date {
  const d = baseDate ? new Date(baseDate) : new Date();
  d.setHours(Math.floor(minuteOfDay / 60), minuteOfDay % 60, 0, 0);
  return d;
}

/**
 * finds and returns all minutes of the day (0-1439) where user input was detected
 * @param date - date to check
 * @returns a Set of active minutes for the given day; minute encoded from 0 to 1439
 */
async function getActiveMinutesSet(date: Date | string): Promise<Set<number>> {
  const d = typeof date === 'string' ? new Date(date) : date;
  const daystr = d.toISOString().split('T')[0]; // e.g 2025-02-28
  // get all user input entries for the day in local timezone
  const userInputToday = await UserInputEntity.createQueryBuilder('userInput')
    .select([
      'userInput.*',
      // Convert UTC timestamp to local time using strftime
      "datetime(userInput.tsStart, 'localtime') as tsStart"
    ])
    .where("date(userInput.tsStart, 'localtime') = :daystr", { daystr })
    .orderBy('userInput.tsStart', 'ASC')
    .getRawMany();

  // set includes all entries with at least one user input
  const activeMinutesSet: Set<number> = new Set();
  userInputToday.forEach((el) => {
    if (el.clickTotal > 0 || el.keysTotal > 0 || el.scrollDelta > 0 || el.movedDistance > 0) {
      activeMinutesSet.add(getMinuteOfDay(new Date(el.tsStart)));
    }
  });

  return activeMinutesSet;
}

/**
 * @param date - date to get window activities for
 * @returns all window activities for the given day
 */
export async function getWindowActivities(date: Date | string): Promise<WindowActivityEntity[]> {
  const d = typeof date === 'string' ? new Date(date) : date;
  const daystr = d.toISOString().split('T')[0];

  const res = await WindowActivityEntity.createQueryBuilder('windowActivity')
    .select(['windowActivity.*', "datetime(windowActivity.ts, 'localtime') as ts"])
    .where("date(windowActivity.ts, 'localtime') = :daystr", { daystr })
    .orderBy('windowActivity.ts', 'ASC')
    .getRawMany();

  return res.map((el) => ({ ...el, ts: new Date(el.ts) })); // convert ts to Date object
}

/**
 * adds an entry to the sessions map for a given key
 * @param map - the map to add the entry to
 * @param key - the key to group by (e.g., processName or activity)
 * @param from - the start date of the session
 * @param to - the end date of the session
 */
function addActivitySessionEntry(
  map: Map<string, ActivitySessions>,
  key: string | null,
  from: Date,
  to: Date,
  activity?: string
) {
  if (!key) {
    return;
  }
  const entry =
    map.get(key) || ({ type: key, totalDurationMs: 0, sessions: [] } as ActivitySessions);
  const duration = to.getTime() - from.getTime();
  const session = { from, to, duration };
  if (activity && !entry.activity) {
    entry.activity = activity;
  }
  entry.sessions.push(session);
  entry.totalDurationMs += duration;
  map.set(key, entry);
}

function addSessionWithActiveMinuteSplits(
  addEntry: (key: string | null, from: Date, to: Date, activity?: string | undefined) => void,
  sessionKey: string | null,
  from: Date,
  to: Date,
  activity: string,
  activeMinutesSet: Set<number>,
  date: Date
) {
  if (!sessionKey || to.getTime() <= from.getTime()) {
    return;
  }

  const startMinute = getMinuteOfDay(from);
  const endMinute = getMinuteOfDay(to);

  let sessionStart = from;
  if (startMinute + 1 < endMinute) {
    // Split a window span when user-input data shows inactivity inside it.
    let inSession = true;
    for (let m = startMinute; m < endMinute; m++) {
      if (!activeMinutesSet.has(m) && inSession) {
        inSession = false;
        let sessionEnd = getDateFromMinuteOfDay(m, date);
        sessionEnd = sessionEnd.getTime() > to.getTime() ? to : sessionEnd;
        addEntry(sessionKey, sessionStart, sessionEnd, activity);
      } else if (!activeMinutesSet.has(m) && !inSession) {
        sessionStart = getDateFromMinuteOfDay(m, date);
      } else if (activeMinutesSet.has(m) && !inSession) {
        sessionStart = getDateFromMinuteOfDay(m, date);
        inSession = true;
      } else if (activeMinutesSet.has(m) && inSession) {
        // session is continuously active
      } else {
        LOG.error('Unexpected state in session reconstruction');
      }
    }
  }

  addEntry(sessionKey, sessionStart, to, activity);
}

/**
 * active window activity sessions grouped by a derived key.
 * @param getSessionKey - derives the session grouping key from a raw window activity entry
 * @returns all usage sessions per prop, including the total duration
 */
async function getWindowActivitySessionsByKey(
  getSessionKey: WindowActivitySessionKeySelector,
  date: Date
): Promise<ActivitySessions[]> {
  const windowActivityToday = await getWindowActivities(date);
  const activeMinutesSet = await getActiveMinutesSet(date);
  // encodes session per processName (=app)
  const sessionsMap: Map<string, ActivitySessions> = new Map();
  // helper function to add an entry to the sessionsMap
  const addEntry = addActivitySessionEntry.bind(undefined, sessionsMap);

  // reconstruct the day so far by iterating over the window activities
  let lastWindowActivity: WindowActivityEntity | undefined = undefined;
  for (const activity of windowActivityToday) {
    if (!activeMinutesSet.has(getMinuteOfDay(new Date(activity.ts)))) {
      // found window activity during a minute without any logged user input
      // skip for safety
      continue;
    }

    const currentSessionKey = getSessionKey(activity);
    if (lastWindowActivity && getSessionKey(lastWindowActivity) !== currentSessionKey) {
      // we found a new window activity for a different app
      const sessionKey = getSessionKey(lastWindowActivity);
      addSessionWithActiveMinuteSplits(
        addEntry,
        sessionKey,
        new Date(lastWindowActivity.ts),
        new Date(activity.ts),
        lastWindowActivity.activity,
        activeMinutesSet,
        date
      );
      lastWindowActivity = activity;
    }

    if (!lastWindowActivity) {
      // initialize the lastWindowActivity in the first iteration
      lastWindowActivity = activity;
    }
  }

  if (lastWindowActivity) {
    const sessionKey = getSessionKey(lastWindowActivity);
    const start = new Date(lastWindowActivity.ts);
    const end = new Date(start);
    end.setMinutes(end.getMinutes() + 1);
    addSessionWithActiveMinuteSplits(
      addEntry,
      sessionKey,
      start,
      end,
      lastWindowActivity.activity,
      activeMinutesSet,
      date
    );
  }

  return Array.from(sessionsMap.values());
}

/**
 * active window activity sessions for apps or activities (e.g., "Planning", "InstantMessaging", ...)
 * @param prop - the property to group by, either "processName" for apps or "activity" for activities
 * @returns all usage sessions per prop, including the total duration
 */
async function getWindowActivitySessionsByType(
  prop: 'processName' | 'activity',
  date: Date
): Promise<ActivitySessions[]> {
  return await getWindowActivitySessionsByKey((activity) => activity[prop], date);
}

function getDomainFromUrl(url: string | null): string | null {
  if (!url) {
    return null;
  }

  try {
    const normalizedUrl = /^[a-z]+:\/\//i.test(url) ? url : `https://${url}`;
    const hostname = new URL(normalizedUrl).hostname;
    return hostname.replace(/^www\./, '') || null;
  } catch (error) {
    LOG.warn('Could not extract domain from URL', url, error);
    return null;
  }
}

function isGenericBrowserTitle(title: string): boolean {
  return (
    /^(\d+\s+)?(or more\s+)?pages?$/i.test(title) ||
    /^(new tab|about:blank|start page|untitled)$/i.test(title)
  );
}

function getReadableUrlTitle(title: string): string | null {
  if (!/^[\w.-]+\.[a-z]{2,}(?:[/:?#]|$)/i.test(title)) {
    return null;
  }

  try {
    const normalizedTitle = /^[a-z]+:\/\//i.test(title) ? title : `https://${title}`;
    const parsedUrl = new URL(normalizedTitle);
    const hostname = parsedUrl.hostname.replace(/^www\./, '');
    const pathParts = parsedUrl.pathname.split('/').filter(Boolean);
    const relevantPath = pathParts.slice(-2).join('/');
    return relevantPath ? `${hostname}/${relevantPath}` : hostname;
  } catch (error) {
    LOG.warn('Could not clean URL-like window title', title, error);
    return null;
  }
}

function stripPathFragment(fragment: string): string {
  const readableUrlTitle = getReadableUrlTitle(fragment);
  if (readableUrlTitle) {
    return readableUrlTitle;
  }

  return fragment.replace(/(?:~|\/Users\/|\/[A-Za-z0-9._-]+|[A-Za-z]:\\)[^\s|]+/g, (path) => {
    const normalizedPath = path.replace(/\\/g, '/');
    const pathParts = normalizedPath.split('/').filter(Boolean);
    return pathParts.at(-1) || path;
  });
}

function cleanWindowTitle(
  windowTitle: string | null,
  processName: string | null,
  url: string | null = null
): string | null {
  if (!windowTitle) {
    return null;
  }

  let title = windowTitle.trim();
  if (!title) {
    return null;
  }

  const suffixNames = [processName, ...BROWSER_PROCESS_NAMES].filter(Boolean) as string[];
  const segments = title
    .split(/\s+(?:-|—|–|\|)\s+/)
    .map((segment) => stripPathFragment(segment.trim()))
    .filter(Boolean);

  while (
    segments.length > 1 &&
    suffixNames.some((suffixName) => segments.at(-1)?.toLowerCase() === suffixName.toLowerCase())
  ) {
    segments.pop();
  }

  while (segments.length > 1 && isGenericBrowserTitle(segments[0])) {
    segments.shift();
  }

  title = segments.length ? segments.join(' - ') : stripPathFragment(title);

  if (isGenericBrowserTitle(title)) {
    return getDomainFromUrl(url);
  }

  return title || null;
}

function isRelevantTopItem(session: ActivitySessions): boolean {
  const normalizedType = session.type.trim().toLowerCase();
  return (
    normalizedType !== 'other' &&
    normalizedType !== 'unknown' &&
    normalizedType !== 'idle' &&
    !isGenericBrowserTitle(session.type)
  );
}

/**
 * finds the longest time period of the given day where user input was detected continuously
 * @returns the longest active time period
 */
export async function getLongestTimeActiveInsight(date: Date): Promise<TimeActive> {
  const activeMinutesSet = await getActiveMinutesSet(date); // encoded from 0 to 1439

  let longest: TimeActive = { from: new Date(), to: new Date(), duration: -1 };
  let periodStart: number | undefined = undefined;
  for (let m = 0; m < 24 * 60; m++) {
    if (activeMinutesSet.has(m) && !periodStart) {
      periodStart = m;
    } else if (!activeMinutesSet.has(m) && periodStart) {
      const duration = m - periodStart;
      if (duration > longest.duration) {
        longest = {
          from: getDateFromMinuteOfDay(periodStart, date),
          to: getDateFromMinuteOfDay(m, date),
          duration
        };
      }
      periodStart = undefined;
    }
  }

  return longest;
}

/**
 * app usage sessions of the given day
 * @returns all usage sessions per app and the total duration
 */
export async function getAppUsageSessions(date: Date): Promise<ActivitySessions[]> {
  return await getWindowActivitySessionsByType('processName', date);
}

export async function getTopWebsiteSessions(date: Date, limit = 3): Promise<ActivitySessions[]> {
  return (
    await getWindowActivitySessionsByKey((activity) => {
      if (activity.activity !== 'WorkRelatedBrowsing') {
        return null;
      }
      return (
        cleanWindowTitle(activity.windowTitle, activity.processName, activity.url) ||
        getDomainFromUrl(activity.url)
      );
    }, date)
  )
    .filter(isRelevantTopItem)
    .sort((a, b) => b.totalDurationMs - a.totalDurationMs)
    .slice(0, limit);
}

export async function getTopWindowTitleSessions(
  date: Date,
  limit = 3
): Promise<ActivitySessions[]> {
  const browsingActivities = new Set([
    'WorkRelatedBrowsing',
    'WorkUnrelatedBrowsing',
    'SocialMedia'
  ]);

  return (
    await getWindowActivitySessionsByKey((activity) => {
      if (browsingActivities.has(activity.activity)) {
        return null;
      }
      return cleanWindowTitle(activity.windowTitle, activity.processName, activity.url);
    }, date)
  )
    .filter(isRelevantTopItem)
    .sort((a, b) => b.totalDurationMs - a.totalDurationMs)
    .slice(0, limit);
}

/**
 * activity sessions of the given day
 * @param date - the day to get activity sessions for
 * @param excludeUnspecificActivities - if true, excludes activities that are not specified (e.g., "Other")
 * @returns all activity sessions per type (e.g., "Planning", "InstantMessaging", ...) and the total duration
 */
export async function getActivitySessions(
  date: Date,
  excludeUnspecificActivities = true
): Promise<ActivitySessions[]> {
  const sessions = await getWindowActivitySessionsByType('activity', date);
  if (excludeUnspecificActivities) {
    return sessions.filter((s) =>
      [
        'DevCode',
        'DevDebug',
        'DevReview',
        'DevVc',
        'Planning',
        'ReadWriteDocument',
        'Design',
        'GenerativeAI',
        'PlannedMeeting',
        'Email',
        'InstantMessaging',
        'WorkRelatedBrowsing',
        'WorkUnrelatedBrowsing',
        'SocialMedia',
        'FileManagement'
      ].includes(s.type)
    );
  }
  return sessions;
}
