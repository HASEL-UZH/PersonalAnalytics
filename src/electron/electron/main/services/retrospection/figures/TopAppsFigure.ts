import type { ActivitySessions } from '../../../../../src/utils/retrospection/types';
import type { RetrospectionSnapshot } from '../RetrospectionSnapshot';
import { addProcessIconsToSessions, type ProcessIconSource } from '../TopItemIcons';
import { getWindowActivitySessionsByType } from '../WindowActivitySessions';

export function buildAppUsageFigure(snapshot: RetrospectionSnapshot): ActivitySessions[] {
  return getWindowActivitySessionsByType(snapshot, 'processName');
}

export async function buildTopAppsFigure(
  snapshot: RetrospectionSnapshot,
  limit = 3
): Promise<ActivitySessions[]> {
  const iconSources = new Map<string, ProcessIconSource>();
  snapshot.windowActivities.forEach((activity) => {
    if (activity.processName && !iconSources.has(activity.processName)) {
      iconSources.set(activity.processName, {
        processName: activity.processName,
        processPath: activity.processPath
      });
    }
  });

  const topApps = buildAppUsageFigure(snapshot)
    .sort((a, b) => b.totalDurationMs - a.totalDurationMs)
    .slice(0, limit);
  return await addProcessIconsToSessions(topApps, (session) => iconSources.get(session.type));
}
