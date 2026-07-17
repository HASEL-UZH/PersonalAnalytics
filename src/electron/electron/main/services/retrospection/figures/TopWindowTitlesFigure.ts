/** Builds and ranks non-browser artifact/window-title sessions for the dashboard. */
import type { ActivitySessions } from '../../../../../src/utils/retrospection/types';
import type { RetrospectionWorkdayData } from '../RetrospectionWorkdayData';
import { addProcessIconsToSessions, type ProcessIconSource } from '../TopItemIcons';
import { getWindowActivitySessionsByKey } from '../WindowActivitySessions';
import {
  cleanWindowTitle,
  EXCLUDED_TOP_WINDOW_TITLE_ACTIVITIES,
  isRelevantTopItem,
  isWebsiteWindowActivity
} from '../WindowTitle';

export async function buildTopWindowTitlesFigure(
  workdayData: RetrospectionWorkdayData,
  limit = 3
): Promise<ActivitySessions[]> {
  const tooltipTitles = new Map<string, string>();
  const iconSources = new Map<string, ProcessIconSource>();
  const topWindowTitles = getWindowActivitySessionsByKey(workdayData, (activity) => {
    if (
      EXCLUDED_TOP_WINDOW_TITLE_ACTIVITIES.has(activity.activity) ||
      isWebsiteWindowActivity(activity)
    ) {
      return null;
    }

    const key = activity.artifactName || cleanWindowTitle(
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
      activity.artifactName ||
      cleanWindowTitle(
        activity.windowTitle,
        activity.processName,
        activity.url,
        true,
        activity.activity
      ) || key
    );
    if (!iconSources.has(key)) {
      iconSources.set(key, {
        processName: activity.processName,
        processPath: activity.processPath
      });
    }
    return key;
  })
    .filter(isRelevantTopItem)
    .sort((a, b) => b.totalDurationMs - a.totalDurationMs)
    .slice(0, limit)
    .map((session) => ({ ...session, tooltipTitle: tooltipTitles.get(session.type) }));
  return await addProcessIconsToSessions(topWindowTitles, (session) =>
    iconSources.get(session.type)
  );
}
