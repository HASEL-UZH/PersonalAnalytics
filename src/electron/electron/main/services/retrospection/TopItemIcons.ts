/**
 * Resolves application icons for top-app, top-website, and top-window-title sessions and attaches
 * renderer-safe data URLs to the corresponding figure data.
 */
import type { ActivitySessions } from '../../../../src/utils/retrospection/types';
import { getProcessIconDataUrl } from '../utils/AppIconHelper';

export interface ProcessIconSource {
  processName: string | null;
  processPath: string | null;
}

/** Adds renderer-safe process icons using the same resolver as activity-timeline hover details. */
export async function addProcessIconsToSessions(
  sessions: ActivitySessions[],
  getIconSource: (session: ActivitySessions) => ProcessIconSource | undefined
): Promise<ActivitySessions[]> {
  return await Promise.all(
    sessions.map(async (session) => {
      const source = getIconSource(session);
      if (!source) {
        return session;
      }

      const iconDataUrl = await getProcessIconDataUrl(source.processPath, source.processName);
      return iconDataUrl ? { ...session, iconDataUrl } : session;
    })
  );
}
