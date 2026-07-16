/**
 * Builds the per-session detail rows shown when hovering an activity-timeline bar. It is specific
 * to the activity timeline; the other figures do not use these hover details.
 */
import type { WindowActivityEntity } from '../../entities/WindowActivityEntity';
import type {
  ActivitySessions,
  TimelineHoverDetail
} from '../../../../src/utils/retrospection/types';
import { getWorkdayMinuteIndex } from '../../../../shared/retrospection/Workday';
import { getProcessIconDataUrl } from '../utils/AppIconHelper';
import { getOverlapDurationMs } from '../utils/helpers';
import type { RetrospectionWorkdayData } from './RetrospectionWorkdayData';
import { getActiveMinuteSpans } from './WindowActivitySessions';
import { getLongTimelineHoverTitle, getShortTimelineHoverTitle } from './WindowTitle';

const TIMELINE_HOVER_DETAIL_LIMIT = 4;
const MIN_TIMELINE_HOVER_DETAIL_DURATION_MS = 60_000;

interface WindowActivityDetailSpan {
  from: Date;
  to: Date;
  activity: string;
  title: string;
  appName?: string | null;
  tooltipTitle?: string;
  iconDataUrl?: string;
}

async function addWindowActivityDetailSpan(
  spans: WindowActivityDetailSpan[],
  activity: WindowActivityEntity,
  from: Date,
  to: Date,
  workdayData: RetrospectionWorkdayData
): Promise<void> {
  const title = getShortTimelineHoverTitle(activity);
  if (!title || to.getTime() <= from.getTime()) {
    return;
  }

  const tooltipTitle = getLongTimelineHoverTitle(activity, title);
  const iconDataUrl = await getProcessIconDataUrl(activity.processPath, activity.processName);
  getActiveMinuteSpans(from, to, workdayData.activeMinutes, workdayData.workdayStart).forEach(
    (span) => {
      spans.push({
        from: span.from,
        to: span.to,
        activity: activity.activity,
        title,
        appName: activity.processName,
        tooltipTitle,
        iconDataUrl
      });
    }
  );
}

async function getWindowActivityDetailSpans(
  workdayData: RetrospectionWorkdayData
): Promise<WindowActivityDetailSpan[]> {
  const spans: WindowActivityDetailSpan[] = [];
  let lastWindowActivity: WindowActivityEntity | undefined;

  for (const activity of workdayData.windowActivities) {
    if (
      !workdayData.activeMinutes.has(
        getWorkdayMinuteIndex(new Date(activity.ts), workdayData.workdayStart)
      )
    ) {
      continue;
    }

    if (lastWindowActivity) {
      await addWindowActivityDetailSpan(
        spans,
        lastWindowActivity,
        new Date(lastWindowActivity.ts),
        new Date(activity.ts),
        workdayData
      );
    }
    lastWindowActivity = activity;
  }

  if (lastWindowActivity) {
    const start = new Date(lastWindowActivity.ts);
    const end = new Date(start);
    end.setMinutes(end.getMinutes() + 1);
    await addWindowActivityDetailSpan(spans, lastWindowActivity, start, end, workdayData);
  }

  return spans;
}

function addActivityTimelineHoverDetailsToSessions(
  activitySessions: ActivitySessions[],
  detailSpans: WindowActivityDetailSpan[],
  limit = TIMELINE_HOVER_DETAIL_LIMIT
): ActivitySessions[] {
  return activitySessions.map((activitySession) => ({
    ...activitySession,
    sessions: activitySession.sessions.map((session) => {
      const detailsByKey = new Map<string, TimelineHoverDetail>();

      detailSpans.forEach((detailSpan) => {
        if (detailSpan.activity !== activitySession.type) {
          return;
        }

        const overlapDurationMs = getOverlapDurationMs(
          session.from,
          session.to,
          detailSpan.from,
          detailSpan.to
        );
        if (overlapDurationMs <= 0) {
          return;
        }

        const detailKey = `${detailSpan.title}\u0000${detailSpan.appName || ''}`;
        const existingDetail = detailsByKey.get(detailKey);
        if (existingDetail) {
          existingDetail.durationMs += overlapDurationMs;
        } else {
          detailsByKey.set(detailKey, {
            title: detailSpan.title,
            appName: detailSpan.appName,
            tooltipTitle: detailSpan.tooltipTitle,
            activity: detailSpan.activity,
            iconDataUrl: detailSpan.iconDataUrl,
            durationMs: overlapDurationMs
          });
        }
      });

      const details = Array.from(detailsByKey.values())
        .filter((detail) => detail.durationMs >= MIN_TIMELINE_HOVER_DETAIL_DURATION_MS)
        .sort((a, b) => b.durationMs - a.durationMs);

      return {
        ...session,
        details: details.slice(0, limit),
        hiddenDetailCount: Math.max(0, details.length - limit)
      };
    })
  }));
}

export async function addActivityTimelineHoverDetails(
  workdayData: RetrospectionWorkdayData,
  activitySessions: ActivitySessions[]
): Promise<ActivitySessions[]> {
  const detailSpans = await getWindowActivityDetailSpans(workdayData);
  return addActivityTimelineHoverDetailsToSessions(activitySessions, detailSpans);
}
