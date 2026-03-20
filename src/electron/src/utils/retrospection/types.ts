export enum DataPointType {
  USER_COMPUTER_ACTIVITY = 'USER_COMPUTER_ACTIVITY',
  TASK_ACTIVITY = 'TASK_ACTIVITY',
  WINDOW_ACTIVITY = 'WINDOW_ACTIVITY'
}

export interface ChartDataPoint {
  start: Date
  end: Date
  color: string
  type: DataPointType
  activity: Activity
}

export interface PieChartDataPoint {
  name: string
  value: number
  color: string
  type?: string
}

export enum Activity {
  Uncategorized = 'Uncategorized',
  DevCode = 'DevCode',
  DevDebug = 'DevDebug',
  DevReview = 'DevReview',
  DevVc = 'DevVc',
  Design = 'Design',
  Email = 'Email',
  GenerativeAI = 'GenerativeAI',
  Planning = 'Planning',
  ReadWriteDocument = 'ReadWriteDocument',
  PlannedMeeting = 'PlannedMeeting',
  InformalMeeting = 'InformalMeeting',
  InstantMessaging = 'InstantMessaging',
  WorkRelatedBrowsing = 'WorkRelatedBrowsing',
  WorkUnrelatedBrowsing = 'WorkUnrelatedBrowsing',
  FileManagement = 'FileManagement',
  SocialMedia = 'SocialMedia',
  Other = 'Other',
  OtherRdp = 'OtherRdp',
  Idle = 'Idle',
  Unknown = 'Unknown'
}

export interface TimeActive {
  from: Date
  to: Date
  duration: number
}

export interface ActivitySessions {
  type: string
  totalDurationMs: number
  sessions: TimeActive[]
}

export enum Color {
  'neutral-800' = '#262626',
  'neutral-400' = '#A3A3A3FF',
  'slate-300' = '#CBD5E1FF',
  'blue-800' = '#1E40AF',
  'violet-800' = '#5B21B6',
  'fuchsia-800' = '#86198F',
  'rose-800' = '#9F1239',
  'orange-800' = '#9A3412',
  'amber-800' = '#92400E',
  'green-800' = '#065F46',
  'teal-800' = '#115E59',
  'blue-600' = '#2563EB',
  'violet-600' = '#7C3AED',
  'fuchsia-600' = '#C026D3',
  'rose-600' = '#E11D48',
  'orange-600' = '#EA580C',
  'amber-600' = '#D97706',
  'green-600' = '#16A34A',
  'teal-600' = '#0D9488',
  'blue-400' = '#60A5FA',
  'violet-400' = '#A78BFA',
  'fuchsia-400' = '#E879F9',
  'rose-400' = '#FB7185',
  'orange-400' = '#FB923C',
  'amber-400' = '#FBBF24',
  'green-400' = '#4ADE80',
  'teal-400' = '#2DD4BF',
  'blue-300' = '#93C5FD',
  'violet-300' = '#C4B5FD',
  'fuchsia-300' = '#F0ABFC',
  'rose-300' = '#FDA4AF',
  'orange-300' = '#FDBA74',
  'amber-300' = '#FCD34D',
  'green-300' = '#6EE7B7',
  'teal-300' = '#5EEAD4',
  'red-400' = '#F87171',
  'red-600' = '#E52E3E',
  'pink-400' = '#F472B6',
  'yellow-400' = '#FACC15',
  'neutral-200' = '#E5E5E5',
  'sky-600' = '#0284c7',
  'sky-400' = '#38bdf8'
}
