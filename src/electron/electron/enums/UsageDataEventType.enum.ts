export enum UsageDataEventType {
  AppStart = 'APP_START',
  AppQuit = 'APP_QUIT',
  SystemLockScreen = 'SYSTEM_LOCK_SCREEN',
  SystemUnlockScreen = 'SYSTEM_UNLOCK_SCREEN',
  SystemSuspend = 'SYSTEM_SUSPEND',
  SystemResume = 'SYSTEM_RESUME',
  SystemShutdown = 'SYSTEM_SHUTDOWN',
  StartExport = 'START_EXPORT',
  FinishExport = 'FINISH_EXPORT',
  ExperienceSamplingManuallyOpened = 'EXPERIENCE_SAMPLING_MANUALLY_OPENED',
  ExperienceSamplingAutomaticallyOpened = 'EXPERIENCE_SAMPLING_AUTOMATICALLY_OPENED',
  ExperienceSamplingAnswered = 'EXPERIENCE_SAMPLING_ANSWERED',
  ExperienceSamplingSkipped = 'EXPERIENCE_SAMPLING_SKIPPED'
}
