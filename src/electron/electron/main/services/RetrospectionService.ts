import { UserInputEntity } from '../entities/UserInputEntity';
import { WindowActivityEntity } from '../entities/WindowActivityEntity';
import { getMainLogger } from '../../config/Logger';
import { Activity } from '../../../src/utils/retrospection/types';

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
  tooltipTitle?: string;
}

type WindowActivitySessionKeySelector = (activity: WindowActivityEntity) => string | null;

// Mirrored from PA.WindowsActivityTracker/typescript/src/mappings/browsers.ts.
// Used here to recognize browser processes without importing tracker source into the main bundle.
const BROWSER_PROCESS_NAME_PARTS = [
  'arc',
  'brave',
  'chrome',
  'chromium',
  'dia',
  'edge',
  'firefox',
  'wavebox',
  'mighty',
  'sigmaos',
  'opera',
  'safari',
  'tor',
  'vivaldi',
  'ecosia',
  'duckduckgo',
  'avg_web_browser'
];

const BROWSER_PROCESS_NAME_ALIASES = new Set(
  [
    ...BROWSER_PROCESS_NAME_PARTS,
    'arc browser',
    'brave browser',
    'dia browser',
    'google chrome',
    'microsoft edge',
    'mozilla firefox',
    'msedge',
    'opera browser',
    'tor browser',
    'vivaldi browser'
  ].map(normalizeProcessName)
);

// Browser window titles often append the user-facing app name, not the process-name token.
// Example: "Pull request review - GitHub - Microsoft Edge" should drop "Microsoft Edge".
const BROWSER_TITLE_SUFFIX_NAMES = [
  'Google Chrome',
  'Microsoft Edge',
  'Mozilla Firefox',
  'Firefox',
  'Safari',
  'Arc',
  'Brave Browser',
  'Opera'
];

const BROWSER_PROFILE_TITLE_NAMES = ['Personal', 'School', 'Work'];

const TOP_WEBSITE_ACTIVITIES = new Set([
  'WorkRelatedBrowsing',
  'DevCode',
  'DevDebug',
  'DevReview',
  'DevVc',
  'ReadWriteDocument',
  'Planning'
]);

const EXCLUDED_TOP_WINDOW_TITLE_ACTIVITIES = new Set([
  'WorkRelatedBrowsing',
  'WorkUnrelatedBrowsing',
  'SocialMedia'
]);

const ACTIVITY_TITLE_SUFFIX_ALIASES: Partial<Record<Activity, string[]>> = {
  [Activity.DevCode]: ['Coding'],
  [Activity.DevDebug]: ['Coding'],
  [Activity.DevReview]: ['Coding'],
  [Activity.DevVc]: ['Coding']
};

/**
 * Normalizes process names so browser aliases can be compared independent of casing or separators.
 * Example: "Microsoft Edge" -> "microsoftedge".
 */
function normalizeProcessName(processName: string): string {
  return processName.toLowerCase().replace(/[^a-z0-9_]+/g, '');
}

export function isBrowserProcessName(processName: string | null): boolean {
  if (!processName) {
    return false;
  }

  const normalizedProcessName = normalizeProcessName(processName);
  if (BROWSER_PROCESS_NAME_ALIASES.has(normalizedProcessName)) {
    return true;
  }

  return BROWSER_PROCESS_NAME_PARTS.some(
    (browser) => normalizedProcessName.includes(normalizeProcessName(browser)) && browser.length > 3
  );
}

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

/**
 * Adds one tracked window span to the aggregate map, but only for minutes where user input exists.
 *
 * The window tracker records "focused window changed at 10:00" and then "focused window changed at
 * 10:30". That implies a 30-minute window span. The user may still have been inactive for part of
 * it, so this method walks the minutes between 10:00 and 10:30 and splits the span around inactive
 * minutes before adding the active pieces.
 *
 * Example: active minutes 10:00-10:10 and 10:20-10:30 become two sessions instead of one 30-minute
 * session.
 */
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
 * Builds active window sessions grouped by a caller-provided session key.
 *
 * The caller decides what counts as "the same thing" by returning a string for each raw window row.
 * For top apps that string is the raw `processName` (for example "Code"). For activity totals it is
 * the raw `activity` (for example "WorkRelatedBrowsing"). For top websites and top window titles,
 * it is a cleaned display label, such as "github.com/pull/123" for browser rows or
 * "Implementation notes" for non-browser rows. Adjacent rows with the same string are
 * combined into one total.
 *
 * Processing steps:
 * 1. Load all window activity rows and active user-input minutes for the selected day.
 * 2. Ignore window rows in minutes where the user was not active.
 * 3. Keep the current raw row open until that grouping string changes.
 * 4. Close the previous row into one or more active-only sessions.
 * 5. Add a one-minute fallback for the final row because there is no next window row to close it.
 *
 * Example: if GitHub is focused from 10:00 until a Code window appears at 10:24, the grouping
 * string "github.com/pull/123" gets a 24-minute session.
 *
 * @param getSessionKey - returns the grouping string for a raw window activity entry
 * @returns all usage sessions for the chosen key, including total active duration
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

/**
 * Extracts the hostname from a URL when the tracker captured one.
 *
 * A bare domain such as "github.com" is often less useful than the visible page title, but it is
 * better than hiding the website row entirely when no usable title is available.
 * Example: "https://github.com/HASEL-UZH/PersonalAnalytics/pull/123" -> "github.com".
 */
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

/**
 * Checks whether a browser title fragment is too generic to display as a useful top item.
 * Example: "New Tab" and "and 2 more pages" are generic, but "Overleaf" is not.
 */
function isGenericBrowserTitle(title: string): boolean {
  return (
    /^((and\s+)?\d+\s+)?((or\s+)?more\s+|other\s+|additional\s+)?pages?$/i.test(title) ||
    /^and\s+((or\s+)?more\s+|other\s+|additional\s+)?pages?$/i.test(title) ||
    /^(new tab|about:blank|start page|untitled)$/i.test(title)
  );
}

/**
 * Removes browser tab-count fragments from otherwise useful page titles.
 * Example: "Overleaf 4 or more pages" -> "Overleaf".
 */
export function removeGenericBrowserTabCountFragments(title: string): string {
  return title
    .replace(/\b(?:and\s+)?(?:\d+\s+)?(?:(?:or\s+)?more|other|additional)\s+pages?\b/gi, '')
    .replace(/\band\s*(?=(?:-|—|–|\||$))/gi, '')
    .replace(/\s{2,}/g, ' ')
    .replace(/^\s*(?:-|—|–|\|)\s*|\s*(?:-|—|–|\|)\s*$/g, '')
    .trim();
}

/**
 * Turns URL-like window titles into a compact, readable label.
 *
 * Some browsers expose a title that is effectively the URL, for example
 * "github.com/HASEL-UZH/PersonalAnalytics/pull/123". For display we keep the domain plus the last
 * two path parts, producing "github.com/pull/123". Hover labels can include "..." to show that the
 * original path was shortened.
 */
export function getReadableUrlTitle(title: string, includeEllipsis = false): string | null {
  if (!/^[\w.-]+\.[a-z]{2,}(?:[/:?#]|$)/i.test(title)) {
    return null;
  }

  try {
    const normalizedTitle = /^[a-z]+:\/\//i.test(title) ? title : `https://${title}`;
    const parsedUrl = new URL(normalizedTitle);
    const hostname = parsedUrl.hostname.replace(/^www\./, '');
    const pathParts = parsedUrl.pathname.split('/').filter(Boolean);
    const relevantPath = pathParts.slice(-2).join('/');
    const ellipsis = includeEllipsis && pathParts.length > 2 ? '.../' : '';
    return relevantPath ? `${hostname}/${ellipsis}${relevantPath}` : hostname;
  } catch (error) {
    LOG.warn('Could not clean URL-like window title', title, error);
    return null;
  }
}

/**
 * Shortens a filesystem path to its final segment, optionally keeping the root plus "...".
 * Example: "C:\Users\username\DevEx" becomes "DevEx", or "C:/.../DevEx" for hover labels.
 */
function shortenPath(path: string, includeEllipsis: boolean): string {
  const normalizedPath = path.replace(/\\/g, '/');
  const pathParts = normalizedPath.split('/').filter(Boolean);
  const fileName = pathParts.at(-1);
  if (!fileName) {
    return path;
  }

  if (!includeEllipsis || pathParts.length < 2) {
    return fileName;
  }

  if (normalizedPath.startsWith('~/')) {
    return `~/.../${fileName}`;
  }

  const windowsDrive = normalizedPath.match(/^[A-Za-z]:\//);
  if (windowsDrive) {
    return `${windowsDrive[0]}.../${fileName}`;
  }

  if (normalizedPath.startsWith('/')) {
    return `/.../${fileName}`;
  }

  return `.../${fileName}`;
}

/**
 * Checks whether a title segment is an app or activity suffix, including versioned app names.
 * Example: "MAXQDA Analytics Pro (26.2.1)" matches the suffix "MAXQDA Analytics Pro".
 */
function isTitleSuffix(segment: string, suffixNames: string[]): boolean {
  const normalizedSegment = normalizeProcessName(segment);
  return suffixNames.some((suffixName) => {
    const normalizedSuffixName = normalizeProcessName(suffixName);
    return (
      normalizedSegment === normalizedSuffixName ||
      (normalizedSegment.startsWith(normalizedSuffixName) && /\(.+\)/.test(segment))
    );
  });
}

/**
 * Builds suffix names from the raw activity ID plus aliases that do not normalize from the ID.
 * Example: "ReadWriteDocument" matches "Read/Write Document"; "DevCode" also matches "Coding".
 */
function getActivityTitleSuffixes(activity: string | null): string[] {
  if (!activity || !(activity in Activity)) {
    return [];
  }

  const activityId = activity as Activity;
  return [activityId, ...(ACTIVITY_TITLE_SUFFIX_ALIASES[activityId] || [])];
}

/**
 * Checks whether a remaining browser segment is only a profile name, not page content.
 * Example: "Personal" is a browser profile label and should not become a top window title.
 */
function isBrowserProfileTitle(title: string): boolean {
  return isTitleSuffix(title, BROWSER_PROFILE_TITLE_NAMES);
}

/**
 * Replaces path-heavy title fragments with the final path segment.
 *
 * Example: "vim ~/code/activitywatch/aw-server/file.py" becomes
 * "vim file.py". Hover labels can keep the root plus "..." as a shortening marker.
 */
export function stripPathFragment(fragment: string, includeEllipsis = false): string {
  const readableUrlTitle = getReadableUrlTitle(fragment, includeEllipsis);
  if (readableUrlTitle) {
    return readableUrlTitle;
  }

  if (/^(?:~\/|\/|[A-Za-z]:[\\/]).+/.test(fragment)) {
    return shortenPath(fragment, includeEllipsis);
  }

  return fragment.replace(
    /(^|\s)((?:~\/|\/Users\/|\/[A-Za-z0-9._-]+|[A-Za-z]:\\)[^\s|]+)/g,
    (_match, prefix, path) => `${prefix}${shortenPath(path, includeEllipsis)}`
  );
}

/**
 * Cleans noisy app/browser window titles before they are used as top-item labels.
 *
 * Substeps:
 * 1. Trim empty titles.
 * 2. Split on common title separators (`-`, `—`, `–`, `|`).
 * 3. Shorten path-like fragments and URL-like fragments.
 * 4. Remove trailing app/browser/activity suffixes such as "Microsoft Edge".
 * 5. Remove generic browser fragments such as "4 or more pages".
 * 6. If the remaining title is still generic, fall back to the captured URL domain.
 *
 * Examples:
 * "4 or more pages - Overleaf - Microsoft Edge" -> "Overleaf"
 * "Overleaf - 4 or more pages - Microsoft Edge" -> "Overleaf"
 * "Overleaf 4 or more pages - Microsoft Edge" -> "Overleaf"
 * "github.com/HASEL-UZH/PersonalAnalytics/pull/123" -> "github.com/pull/123"
 * "vim ~/code/activitywatch/aw-server/file.py" -> "vim file.py"
 */
export function cleanWindowTitle(
  windowTitle: string | null,
  processName: string | null,
  url: string | null = null,
  includeEllipsis = false,
  activity: string | null = null
): string | null {
  if (!windowTitle) {
    return null;
  }

  let title = windowTitle.trim();
  if (!title) {
    return null;
  }

  const suffixNames = [
    processName,
    ...(isBrowserProcessName(processName) ? BROWSER_TITLE_SUFFIX_NAMES : []),
    ...getActivityTitleSuffixes(activity)
  ].filter(Boolean) as string[];
  const isBrowserTitle = isBrowserProcessName(processName) || !!url;
  const segments = title
    .split(/\s+(?:-|—|–|\|)\s+/)
    .map((segment) =>
      stripPathFragment(
        isBrowserTitle ? removeGenericBrowserTabCountFragments(segment.trim()) : segment.trim(),
        includeEllipsis
      )
    )
    .filter(Boolean);

  while (segments.length > 1 && isTitleSuffix(segments.at(-1) || '', suffixNames)) {
    segments.pop();
  }

  if (segments.length > 1) {
    const meaningfulSegments = segments.filter(
      (segment) => !isGenericBrowserTitle(segment) && !isBrowserProfileTitle(segment)
    );
    if (meaningfulSegments.length > 0) {
      segments.splice(0, segments.length, ...meaningfulSegments);
    } else {
      return getDomainFromUrl(url);
    }
  }

  title = segments.length ? segments.join(' - ') : stripPathFragment(title, includeEllipsis);

  if (isGenericBrowserTitle(title)) {
    return getDomainFromUrl(url);
  }

  if (isTitleSuffix(title, suffixNames)) {
    return getDomainFromUrl(url);
  }

  if (isBrowserTitle && isBrowserProfileTitle(title)) {
    return getDomainFromUrl(url);
  }

  return title || null;
}

/**
 * Checks whether a raw activity category should contribute to the Top websites card.
 */
function isTopWebsiteActivity(activity: WindowActivityEntity): boolean {
  return TOP_WEBSITE_ACTIVITIES.has(activity.activity);
}

/**
 * Checks whether an activity should be treated as a website row instead of a native window-title row.
 */
function isWebsiteWindowActivity(activity: WindowActivityEntity): boolean {
  return (
    isTopWebsiteActivity(activity) && (isBrowserProcessName(activity.processName) || !!activity.url)
  );
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
  const tooltipTitles = new Map<string, string>();
  return (
    await getWindowActivitySessionsByKey((activity) => {
      if (!isWebsiteWindowActivity(activity)) {
        return null;
      }
      const key =
        cleanWindowTitle(
          activity.windowTitle,
          activity.processName,
          activity.url,
          false,
          activity.activity
        ) || getDomainFromUrl(activity.url);
      if (!key) {
        return null;
      }
      tooltipTitles.set(
        key,
        cleanWindowTitle(
          activity.windowTitle,
          activity.processName,
          activity.url,
          true,
          activity.activity
        ) || key
      );
      return key;
    }, date)
  )
    .filter(isRelevantTopItem)
    .sort((a, b) => b.totalDurationMs - a.totalDurationMs)
    .slice(0, limit)
    .map((session) => ({ ...session, tooltipTitle: tooltipTitles.get(session.type) }));
}

export async function getTopWindowTitleSessions(
  date: Date,
  limit = 3
): Promise<ActivitySessions[]> {
  const tooltipTitles = new Map<string, string>();
  return (
    await getWindowActivitySessionsByKey((activity) => {
      if (
        EXCLUDED_TOP_WINDOW_TITLE_ACTIVITIES.has(activity.activity) ||
        isWebsiteWindowActivity(activity)
      ) {
        return null;
      }
      const key = cleanWindowTitle(
        activity.windowTitle,
        activity.processName,
        activity.url,
        false,
        activity.activity
      );
      if (!key) {
        return null;
      }
      tooltipTitles.set(
        key,
        cleanWindowTitle(
          activity.windowTitle,
          activity.processName,
          activity.url,
          true,
          activity.activity
        ) || key
      );
      return key;
    }, date)
  )
    .filter(isRelevantTopItem)
    .sort((a, b) => b.totalDurationMs - a.totalDurationMs)
    .slice(0, limit)
    .map((session) => ({ ...session, tooltipTitle: tooltipTitles.get(session.type) }));
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
