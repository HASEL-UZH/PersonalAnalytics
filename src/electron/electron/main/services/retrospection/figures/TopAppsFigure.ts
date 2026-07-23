/** Builds process-based app-usage sessions and selects the most-used apps for the dashboard. */
import type { ActivitySessions } from '../../../../../src/utils/retrospection/types';
import type { RetrospectionWorkdayData } from '../RetrospectionWorkdayData';
import { addProcessIconsToSessions, type ProcessIconSource } from '../TopItemIcons';
import { getWindowActivitySessionsByType } from '../WindowActivitySessions';

export function buildAppUsageFigure(workdayData: RetrospectionWorkdayData): ActivitySessions[] {
  return getWindowActivitySessionsByType(workdayData, 'processName');
}

export async function buildTopAppsFigure(
  workdayData: RetrospectionWorkdayData,
  limit = 3
): Promise<ActivitySessions[]> {
  const iconSources = new Map<string, ProcessIconSource>();
  workdayData.windowActivities.forEach((activity) => {
    if (activity.processName && !iconSources.has(activity.processName)) {
      iconSources.set(activity.processName, {
        processName: activity.processName,
        processPath: activity.processPath,
        processId: activity.processId
      });
    }
  });

  const topApps = buildAppUsageFigure(workdayData)
    .sort((a, b) => b.totalDurationMs - a.totalDurationMs)
    .slice(0, limit);
  return await addProcessIconsToSessions(topApps, (session) => iconSources.get(session.type));
}
