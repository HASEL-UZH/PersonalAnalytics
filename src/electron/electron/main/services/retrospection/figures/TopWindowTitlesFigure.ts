import type { ActivitySessions } from '../../../../../src/utils/retrospection/types';
import type { RetrospectionSnapshot } from '../RetrospectionSnapshot';
import { addProcessIconsToSessions, type ProcessIconSource } from '../TopItemIcons';
import { getWindowActivitySessionsByKey } from '../WindowActivitySessions';
import {
  cleanWindowTitle,
  EXCLUDED_TOP_WINDOW_TITLE_ACTIVITIES,
  isRelevantTopItem,
  isWebsiteWindowActivity
} from '../WindowTitle';

export async function buildTopWindowTitlesFigure(
  snapshot: RetrospectionSnapshot,
  limit = 3
): Promise<ActivitySessions[]> {
  const tooltipTitles = new Map<string, string>();
  const iconSources = new Map<string, ProcessIconSource>();
  const topWindowTitles = getWindowActivitySessionsByKey(snapshot, (activity) => {
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
