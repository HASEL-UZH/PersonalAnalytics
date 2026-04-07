import { Color, Activity } from './types'

export function msToReadableFormat(
  durationInMs: number,
  flashColon: boolean = false,
  showSeconds: boolean = false
): string {
  const flash: ':' | ' ' = flashColon ? (Math.floor(Date.now() / 1000) % 2 === 0 ? ':' : ' ') : ':'

  const hours = Math.floor(durationInMs / (1000 * 60 * 60))
  const minutes = Math.floor((durationInMs % (1000 * 60 * 60)) / (1000 * 60))
  const seconds = Math.floor((durationInMs % (1000 * 60)) / 1000)

  const formattedHours = String(hours).padStart(2, '0')
  const formattedMinutes = String(minutes).padStart(2, '0')
  const formattedSeconds = String(seconds).padStart(2, '0')

  let formatWithoutSeconds = `${formattedHours}${flash}${formattedMinutes}`
  if (formatWithoutSeconds === '00:00') {
    formatWithoutSeconds = '< 00:01'
  }

  return showSeconds
    ? `${formattedHours}${flash}${formattedMinutes}${flash}${formattedSeconds}`
    : `${formatWithoutSeconds}`
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
}

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
  Idle: 'neutral-400',
}

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
}

export function getBarColorFromDataPoint(tailwindColorClass: string): string {
  return (Color as any)[tailwindColorClass]
}

export function getActivityGroupFromActivityName(activityName: string): string {
  const activityGroup: string | undefined = Object.keys(ACTIVITY_GROUPS).find((group) =>
    ACTIVITY_GROUPS[group].includes(activityName as Activity)
  )
  return activityGroup || 'Other'
}

export function getTailwindClassFromActivity(activityName: string, isGroup = false): string {
  if (isGroup) {
    return TW_CLASS_ACTIVITY_MAPPINGS[activityName]
  }
  const activityGroup = getActivityGroupFromActivityName(activityName)
  return TW_CLASS_ACTIVITY_MAPPINGS[activityGroup]
}
