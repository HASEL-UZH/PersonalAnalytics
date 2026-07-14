import { getMainLogger } from '../../config/Logger';
import type { WindowActivityEntity } from '../entities/WindowActivityEntity';
import type {
  ActiveHoursInsight,
  ActivitySessions,
  RetrospectionDataSection,
  TimeActive
} from '../../../src/utils/retrospection/types';
import {
  getWindowActivitiesForWorkday,
  loadRetrospectionSnapshot,
  type RetrospectionSnapshot
} from './retrospection/RetrospectionSnapshot';
import {
  buildActiveHoursFigure,
  buildLongestActivePeriodFigure
} from './retrospection/figures/ActiveHoursFigure';
import {
  buildActivityTimelineFigure,
  isSpecificRetrospectionActivity
} from './retrospection/figures/ActivityTimelineFigure';
import { buildAppUsageFigure, buildTopAppsFigure } from './retrospection/figures/TopAppsFigure';
import { buildTopWebsitesFigure } from './retrospection/figures/TopWebsitesFigure';
import { buildTopWindowTitlesFigure } from './retrospection/figures/TopWindowTitlesFigure';

const LOG = getMainLogger('RetrospectionService');

const ACTIVITY_SECTION_ORDER: RetrospectionDataSection[] = [
  'activities',
  'activeHours',
  'longestActivePeriod',
  'topApps',
  'topWebsites',
  'topWindowTitles'
];

export interface RetrospectionActivityDashboard {
  activities: ActivitySessions[];
  activeHours?: ActiveHoursInsight;
  longestActivePeriod?: TimeActive;
  topApps: ActivitySessions[];
  topWebsites: ActivitySessions[];
  topWindowTitles: ActivitySessions[];
  errors: RetrospectionDataSection[];
}

function emptyActivityDashboard(
  errors: RetrospectionDataSection[] = []
): RetrospectionActivityDashboard {
  return {
    activities: [],
    activeHours: undefined,
    longestActivePeriod: undefined,
    topApps: [],
    topWebsites: [],
    topWindowTitles: [],
    errors
  };
}

/**
 * Builds every tracker-backed figure from one shared workday snapshot.
 * A figure failure is isolated to its own section, while a snapshot failure marks all activity
 * sections unavailable because none of them can be calculated without the raw tracker data.
 */
export async function getRetrospectionActivityDashboard(
  date: Date
): Promise<RetrospectionActivityDashboard> {
  let snapshot: RetrospectionSnapshot;
  try {
    snapshot = await loadRetrospectionSnapshot(date);
  } catch (error) {
    LOG.error('Error loading retrospection snapshot', error);
    return emptyActivityDashboard([...ACTIVITY_SECTION_ORDER]);
  }

  const errors = new Set<RetrospectionDataSection>();
  const buildFigure = async <T>(
    section: RetrospectionDataSection,
    builder: () => T | Promise<T>,
    fallback: T
  ): Promise<T> => {
    try {
      return await builder();
    } catch (error) {
      errors.add(section);
      LOG.error(`Error building retrospection figure: ${section}`, error);
      return fallback;
    }
  };

  const [activities, activeHours, longestActivePeriod, topApps, topWebsites, topWindowTitles] =
    await Promise.all([
      buildFigure('activities', () => buildActivityTimelineFigure(snapshot), []),
      buildFigure('activeHours', () => buildActiveHoursFigure(snapshot), undefined),
      buildFigure('longestActivePeriod', () => buildLongestActivePeriodFigure(snapshot), undefined),
      buildFigure('topApps', () => buildTopAppsFigure(snapshot), []),
      buildFigure('topWebsites', () => buildTopWebsitesFigure(snapshot), []),
      buildFigure('topWindowTitles', () => buildTopWindowTitlesFigure(snapshot), [])
    ]);

  return {
    activities,
    activeHours,
    longestActivePeriod,
    topApps,
    topWebsites,
    topWindowTitles,
    errors: ACTIVITY_SECTION_ORDER.filter((section) => errors.has(section))
  };
}

// Compatibility façade for callers and focused tests. Dashboard IPC uses the shared snapshot above.
export const getWindowActivities = getWindowActivitiesForWorkday;

export async function getActiveHoursInsight(date: Date): Promise<ActiveHoursInsight> {
  return buildActiveHoursFigure(await loadRetrospectionSnapshot(date));
}

export async function getLongestTimeActiveInsight(date: Date): Promise<TimeActive> {
  return buildLongestActivePeriodFigure(await loadRetrospectionSnapshot(date));
}

export async function getAppUsageSessions(date: Date): Promise<ActivitySessions[]> {
  return buildAppUsageFigure(await loadRetrospectionSnapshot(date));
}

export async function getTopWebsiteSessions(date: Date, limit = 3): Promise<ActivitySessions[]> {
  return buildTopWebsitesFigure(await loadRetrospectionSnapshot(date), limit);
}

export async function getTopWindowTitleSessions(
  date: Date,
  limit = 3
): Promise<ActivitySessions[]> {
  return buildTopWindowTitlesFigure(await loadRetrospectionSnapshot(date), limit);
}

export async function getActivitySessions(
  date: Date,
  excludeUnspecificActivities = true
): Promise<ActivitySessions[]> {
  return await buildActivityTimelineFigure(
    await loadRetrospectionSnapshot(date),
    excludeUnspecificActivities
  );
}

export { isSpecificRetrospectionActivity };
export {
  cleanWindowTitle,
  getReadableUrlTitle,
  isBrowserProcessName,
  removeGenericBrowserTabCountFragments,
  stripPathFragment
} from './retrospection/WindowTitle';
export type {
  ActivitySessions,
  TimeActive,
  TimelineHoverDetail
} from '../../../src/utils/retrospection/types';
export type { WindowActivityEntity };
