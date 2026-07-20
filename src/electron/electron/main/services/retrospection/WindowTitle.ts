/**
 * Normalizes window titles and URLs into readable labels used by timeline hover details and the
 * top-website and top-window-title figures.
 */
import { getMainLogger } from '../../../config/Logger';
import type { WindowActivityEntity } from '../../entities/WindowActivityEntity';
import { Activity, type ActivitySessions } from '../../../../src/utils/retrospection/types';

const LOG = getMainLogger('RetrospectionWindowTitle');

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
export const EXCLUDED_TOP_WINDOW_TITLE_ACTIVITIES = new Set([
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

export function getDomainFromUrl(url: string | null): string | null {
  if (!url) {
    return null;
  }

  try {
    const normalizedUrl = /^[a-z]+:\/\//i.test(url) ? url : `https://${url}`;
    return new URL(normalizedUrl).hostname.replace(/^www\./, '') || null;
  } catch (error) {
    LOG.warn('Could not extract domain from URL', url, error);
    return null;
  }
}

function isGenericBrowserTitle(title: string): boolean {
  return (
    /^((and\s+)?\d+\s+)?((or\s+)?more\s+|other\s+|additional\s+)?pages?$/i.test(title) ||
    /^and\s+((or\s+)?more\s+|other\s+|additional\s+)?pages?$/i.test(title) ||
    /^(new tab|about:blank|start page|untitled)$/i.test(title)
  );
}

export function removeGenericBrowserTabCountFragments(title: string): string {
  return title
    .replace(/\b(?:and\s+)?(?:\d+\s+)?(?:(?:or\s+)?more|other|additional)\s+pages?\b/gi, '')
    .replace(/\band\s*(?=(?:-|—|–|\||$))/gi, '')
    .replace(/\s{2,}/g, ' ')
    .replace(/^\s*(?:-|—|–|\|)\s*|\s*(?:-|—|–|\|)\s*$/g, '')
    .trim();
}

export function getReadableUrlTitle(title: string, includeEllipsis = false): string | null {
  if (!/^[\w.-]+\.[a-z]{2,}(?:\/|[?#]|:\d|$)/i.test(title)) {
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

function getActivityTitleSuffixes(activity: string | null): string[] {
  if (!activity || !(activity in Activity)) {
    return [];
  }
  const activityId = activity as Activity;
  return [activityId, ...(ACTIVITY_TITLE_SUFFIX_ALIASES[activityId] || [])];
}

function isBrowserProfileTitle(title: string): boolean {
  return isTitleSuffix(title, BROWSER_PROFILE_TITLE_NAMES);
}

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

export function cleanWindowTitle(
  windowTitle: string | null,
  processName: string | null,
  url: string | null = null,
  includeEllipsis = false,
  activity: string | null = null
): string | null {
  if (!windowTitle?.trim()) {
    return null;
  }

  let title = windowTitle.trim();
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
    if (meaningfulSegments.length === 0) {
      return getDomainFromUrl(url);
    }
    segments.splice(0, segments.length, ...meaningfulSegments);
  }

  title = segments.length ? segments.join(' - ') : stripPathFragment(title, includeEllipsis);
  if (
    isGenericBrowserTitle(title) ||
    isTitleSuffix(title, suffixNames) ||
    (isBrowserTitle && isBrowserProfileTitle(title))
  ) {
    return getDomainFromUrl(url);
  }
  return title || null;
}

export function isWebsiteWindowActivity(activity: WindowActivityEntity): boolean {
  return (
    TOP_WEBSITE_ACTIVITIES.has(activity.activity) &&
    (isBrowserProcessName(activity.processName) || !!activity.url)
  );
}

export function isRelevantTopItem(session: ActivitySessions): boolean {
  const normalizedType = session.type.trim().toLowerCase();
  return (
    normalizedType !== 'other' &&
    normalizedType !== 'unknown' &&
    normalizedType !== 'idle' &&
    !isGenericBrowserTitle(session.type)
  );
}

export function getShortTimelineHoverTitle(activity: WindowActivityEntity): string | null {
  return (
    activity.artifactName ||
    cleanWindowTitle(
      activity.windowTitle,
      activity.processName,
      activity.url,
      false,
      activity.activity
    ) ||
    getDomainFromUrl(activity.url) ||
    activity.processName ||
    activity.activity
  );
}

export function getLongTimelineHoverTitle(
  activity: WindowActivityEntity,
  fallbackTitle: string
): string {
  return (
    activity.artifactName ||
    cleanWindowTitle(
      activity.windowTitle,
      activity.processName,
      activity.url,
      true,
      activity.activity
    ) || fallbackTitle
  );
}
