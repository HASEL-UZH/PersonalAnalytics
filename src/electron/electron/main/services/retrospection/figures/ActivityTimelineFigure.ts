/** Builds the grouped activity timeline and enriches its sessions with hover details. */
import { Activity, type ActivitySessions } from '../../../../../src/utils/retrospection/types';
import type { RetrospectionWorkdayData } from '../RetrospectionWorkdayData';
import { addActivityTimelineHoverDetails } from '../ActivityTimelineDetails';
import { getWindowActivitySessionsByType } from '../WindowActivitySessions';

// Activity also contains unspecific states such as Other, Idle, Unknown, and Uncategorized. This
// explicit subset is the set that should remain visible in retrospection figures.
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
  workdayData: RetrospectionWorkdayData,
  excludeUnspecificActivities = true
): Promise<ActivitySessions[]> {
  const sessions = getWindowActivitySessionsByType(workdayData, 'activity');
  const filteredSessions = excludeUnspecificActivities
    ? sessions.filter((session) => isSpecificRetrospectionActivity(session.type))
    : sessions;
  return await addActivityTimelineHoverDetails(workdayData, filteredSessions);
}
