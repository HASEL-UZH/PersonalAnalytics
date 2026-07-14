import { Activity, type ActivitySessions } from '../../../../../src/utils/retrospection/types';
import type { RetrospectionSnapshot } from '../RetrospectionSnapshot';
import { addTimelineHoverDetails } from '../TimelineDetails';
import { getWindowActivitySessionsByType } from '../WindowActivitySessions';

const SPECIFIC_ACTIVITY_TYPES = new Set<Activity>([
  Activity.DevCode,
  Activity.DevDebug,
  Activity.DevReview,
  Activity.DevVc,
  Activity.Planning,
  Activity.ReadWriteDocument,
  Activity.Design,
  Activity.GenerativeAI,
  Activity.PlannedMeeting,
  Activity.InformalMeeting,
  Activity.Email,
  Activity.InstantMessaging,
  Activity.WorkRelatedBrowsing,
  Activity.WorkUnrelatedBrowsing,
  Activity.SocialMedia,
  Activity.FileManagement
]);

export function isSpecificRetrospectionActivity(activityType: string): boolean {
  return SPECIFIC_ACTIVITY_TYPES.has(activityType as Activity);
}

export async function buildActivityTimelineFigure(
  snapshot: RetrospectionSnapshot,
  excludeUnspecificActivities = true
): Promise<ActivitySessions[]> {
  const sessions = getWindowActivitySessionsByType(snapshot, 'activity');
  const filteredSessions = excludeUnspecificActivities
    ? sessions.filter((session) => isSpecificRetrospectionActivity(session.type))
    : sessions;
  return await addTimelineHoverDetails(snapshot, filteredSessions);
}
