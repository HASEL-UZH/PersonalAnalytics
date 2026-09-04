import { Color, Activity } from './types';

export function formatDuration(durationInMs: number): string {
  if (!Number.isFinite(durationInMs) || durationInMs <= 0) {
    return '0 mins';
  }

  const totalMinutes = Math.round(durationInMs / 60_000);
  if (totalMinutes < 1) {
    return '< 1 min';
  }
  if (totalMinutes < 60) {
    return `${totalMinutes} ${totalMinutes === 1 ? 'min' : 'mins'}`;
  }

  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  const formattedHours = `${hours} ${hours === 1 ? 'hr' : 'hrs'}`;
  return minutes === 0
    ? formattedHours
    : `${formattedHours} ${minutes} ${minutes === 1 ? 'min' : 'mins'}`;
}

const ACTIVITY_GROUPS: Record<string, Activity[]> = {
  Development: [Activity.DevCode, Activity.DevDebug, Activity.DevReview, Activity.DevVc],
  Planning: [Activity.Planning],
  ReadWriteDocument: [Activity.ReadWriteDocument],
  Design: [Activity.Design],
  GenerativeAI: [Activity.GenerativeAI],
  Meeting: [Activity.PlannedMeeting, Activity.InformalMeeting],
  Email: [Activity.Email],
  InstantMessaging: [Activity.InstantMessaging],
  WorkRelatedBrowsing: [Activity.WorkRelatedBrowsing],
  WorkUnrelatedBrowsing: [Activity.WorkUnrelatedBrowsing],
  SocialMedia: [Activity.SocialMedia],
  FileManagement: [Activity.FileManagement],
  Other: [Activity.Unknown, Activity.Other, Activity.OtherRdp],
  Idle: [Activity.Idle]
};

export const TW_CLASS_ACTIVITY_MAPPINGS: Record<string, string> = {
  Development: 'sky-400',
  Planning: 'orange-400',
  ReadWriteDocument: 'teal-400',
  Design: 'green-300',
  GenerativeAI: 'orange-300',
  Meeting: 'violet-400',
  Email: 'violet-600',
  InstantMessaging: 'red-400',
  WorkRelatedBrowsing: 'green-600',
  WorkUnrelatedBrowsing: 'red-600',
  SocialMedia: 'red-600',
  FileManagement: 'teal-600',
  Other: 'neutral-400',
  Idle: 'neutral-400'
};

export const ACTIVITY_LABELS: Record<string, string> = {
  Development: 'Coding',
  Planning: 'Planning',
  ReadWriteDocument: 'Read/Write Document',
  Design: 'Design',
  GenerativeAI: 'Generative AI',
  Meeting: 'Meeting',
  Email: 'Email',
  InstantMessaging: 'Instant Messaging',
  WorkRelatedBrowsing: 'Work Related Browsing',
  WorkUnrelatedBrowsing: 'Work Unrelated Browsing',
  SocialMedia: 'Social Media',
  FileManagement: 'File Management',
  Other: 'Other',
  Idle: 'Idle'
};

export function getBarColorFromDataPoint(tailwindColorClass: string): string {
  return (Color as any)[tailwindColorClass];
}

export function getActivityGroupFromActivityName(activityName: string): string {
  const activityGroup: string | undefined = Object.keys(ACTIVITY_GROUPS).find((group) =>
    ACTIVITY_GROUPS[group].includes(activityName as Activity)
  );
  return activityGroup || 'Other';
}

export function getTailwindClassFromActivity(activityName: string, isGroup = false): string {
  if (isGroup) {
    return TW_CLASS_ACTIVITY_MAPPINGS[activityName];
  }
  const activityGroup = getActivityGroupFromActivityName(activityName);
  return TW_CLASS_ACTIVITY_MAPPINGS[activityGroup];
}

export function escapeHtml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

export function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max);
}
