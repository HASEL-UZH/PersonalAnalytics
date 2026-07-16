/** Builds and ranks website sessions from browser-related window activity. */
import type { ActivitySessions } from '../../../../../src/utils/retrospection/types';
import type { RetrospectionWorkdayData } from '../RetrospectionWorkdayData';
import { addProcessIconsToSessions, type ProcessIconSource } from '../TopItemIcons';
import { getWindowActivitySessionsByKey } from '../WindowActivitySessions';
import {
  cleanWindowTitle,
  getDomainFromUrl,
  isRelevantTopItem,
  isWebsiteWindowActivity
} from '../WindowTitle';

export async function buildTopWebsitesFigure(
  workdayData: RetrospectionWorkdayData,
  limit = 3
): Promise<ActivitySessions[]> {
  const tooltipTitles = new Map<string, string>();
  const iconSources = new Map<string, ProcessIconSource>();
  const topWebsites = getWindowActivitySessionsByKey(workdayData, (activity) => {
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
  return await addProcessIconsToSessions(topWebsites, (session) => iconSources.get(session.type));
}
