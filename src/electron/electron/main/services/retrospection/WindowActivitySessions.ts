import { getMainLogger } from '../../../config/Logger';
import type { WindowActivityEntity } from '../../entities/WindowActivityEntity';
import type { ActivitySessions, TimeActive } from '../../../../src/utils/retrospection/types';
import {
  getDateFromWorkdayMinuteIndex,
  getWorkdayMinuteIndex
} from '../../../../shared/retrospection/Workday';
import type { RetrospectionSnapshot } from './RetrospectionSnapshot';

const LOG = getMainLogger('WindowActivitySessions');

export type WindowActivitySessionKeySelector = (activity: WindowActivityEntity) => string | null;

function addActivitySessionEntry(
  sessions: Map<string, ActivitySessions>,
  key: string | null,
  from: Date,
  to: Date,
  activity?: string
): void {
  if (!key) {
    return;
  }

  const entry = sessions.get(key) ?? { type: key, totalDurationMs: 0, sessions: [] };
  const duration = to.getTime() - from.getTime();
  if (activity && !entry.activity) {
    entry.activity = activity;
  }
  entry.sessions.push({ from, to, duration });
  entry.totalDurationMs += duration;
  sessions.set(key, entry);
}

export function getActiveMinuteSpans(
  from: Date,
  to: Date,
  activeMinutes: Set<number>,
  workdayStart: Date
): TimeActive[] {
  if (to.getTime() <= from.getTime()) {
    return [];
  }

  const spans: TimeActive[] = [];
  const addSpan = (spanFrom: Date, spanTo: Date): void => {
    if (spanTo.getTime() > spanFrom.getTime()) {
      spans.push({
        from: spanFrom,
        to: spanTo,
        duration: spanTo.getTime() - spanFrom.getTime()
      });
    }
  };

  const startMinute = getWorkdayMinuteIndex(from, workdayStart);
  const endMinute = getWorkdayMinuteIndex(to, workdayStart);
  let sessionStart = from;

  if (startMinute + 1 < endMinute) {
    let inSession = true;
    for (let minute = startMinute; minute < endMinute; minute++) {
      if (!activeMinutes.has(minute) && inSession) {
        inSession = false;
        const inactiveMinuteStart = getDateFromWorkdayMinuteIndex(minute, workdayStart);
        addSpan(
          sessionStart,
          inactiveMinuteStart.getTime() > to.getTime() ? to : inactiveMinuteStart
        );
      } else if (!activeMinutes.has(minute) && !inSession) {
        sessionStart = getDateFromWorkdayMinuteIndex(minute, workdayStart);
      } else if (activeMinutes.has(minute) && !inSession) {
        sessionStart = getDateFromWorkdayMinuteIndex(minute, workdayStart);
        inSession = true;
      } else if (!activeMinutes.has(minute) || !inSession) {
        LOG.error('Unexpected state in session reconstruction');
      }
    }
  }

  addSpan(sessionStart, to);
  return spans;
}

function addSessionWithActiveMinuteSplits(
  sessions: Map<string, ActivitySessions>,
  sessionKey: string | null,
  from: Date,
  to: Date,
  activity: string,
  snapshot: RetrospectionSnapshot
): void {
  if (!sessionKey || to.getTime() <= from.getTime()) {
    return;
  }

  getActiveMinuteSpans(from, to, snapshot.activeMinutes, snapshot.workdayStart).forEach((span) => {
    addActivitySessionEntry(sessions, sessionKey, span.from, span.to, activity);
  });
}

/** Reconstructs active-only sessions from a snapshot without performing database queries. */
export function getWindowActivitySessionsByKey(
  snapshot: RetrospectionSnapshot,
  getSessionKey: WindowActivitySessionKeySelector
): ActivitySessions[] {
  const sessions = new Map<string, ActivitySessions>();
  let lastWindowActivity: WindowActivityEntity | undefined;

  for (const activity of snapshot.windowActivities) {
    if (
      !snapshot.activeMinutes.has(
        getWorkdayMinuteIndex(new Date(activity.ts), snapshot.workdayStart)
      )
    ) {
      continue;
    }

    const currentSessionKey = getSessionKey(activity);
    if (lastWindowActivity && getSessionKey(lastWindowActivity) !== currentSessionKey) {
      addSessionWithActiveMinuteSplits(
        sessions,
        getSessionKey(lastWindowActivity),
        new Date(lastWindowActivity.ts),
        new Date(activity.ts),
        lastWindowActivity.activity,
        snapshot
      );
      lastWindowActivity = activity;
    }

    lastWindowActivity ??= activity;
  }

  if (lastWindowActivity) {
    const start = new Date(lastWindowActivity.ts);
    const end = new Date(start);
    end.setMinutes(end.getMinutes() + 1);
    addSessionWithActiveMinuteSplits(
      sessions,
      getSessionKey(lastWindowActivity),
      start,
      end,
      lastWindowActivity.activity,
      snapshot
    );
  }

  return Array.from(sessions.values());
}

export function getWindowActivitySessionsByType(
  snapshot: RetrospectionSnapshot,
  property: 'processName' | 'activity'
): ActivitySessions[] {
  return getWindowActivitySessionsByKey(snapshot, (activity) => activity[property]);
}
