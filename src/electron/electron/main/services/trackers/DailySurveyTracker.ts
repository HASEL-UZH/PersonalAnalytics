import * as schedule from 'node-schedule';
import { WindowService } from '../WindowService';
import { Tracker } from './Tracker';
import getMainLogger from '../../../config/Logger';
import { Settings } from '../../entities/Settings';
import { WorkScheduleService } from '../WorkScheduleService';
import type {
  DailySurveyConfig,
  DailySurveySamplingType
} from '../../../../shared/StudyConfiguration';

const LOG = getMainLogger('DailySurveyTracker');

const weekDays = ['monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday'];
const MAX_MISSED_SURVEY_AGE_DAYS = 3;

type DailySurveyDateField =
  | 'nextDailySurveyMorningInvocation'
  | 'nextDailySurveyEveningInvocation'
  | 'pendingDailySurveyMorningScheduledDate'
  | 'pendingDailySurveyEveningScheduledDate'
  | 'postponedDailySurveyMorningUntil'
  | 'postponedDailySurveyEveningUntil';

export class DailySurveyTracker implements Tracker {
  private checkJob: schedule.Job;
  private shownSurveyDates = new Map<DailySurveySamplingType, number>();
  private activeSurvey: { samplingType: DailySurveySamplingType; scheduledDate: number } | null =
    null;
  private readonly windowService: WindowService;
  private readonly workScheduleService: WorkScheduleService;
  private readonly surveys: DailySurveyConfig[];

  public readonly name: string = 'Daily Survey';
  public isRunning: boolean = false;

  constructor(
    windowService: WindowService,
    workScheduleService: WorkScheduleService,
    surveys: DailySurveyConfig[]
  ) {
    this.windowService = windowService;
    this.workScheduleService = workScheduleService;
    this.surveys = surveys;
  }

  public async start(): Promise<void> {
    try {
      await this.processSurveys();
      this.startCheckJob();
      this.isRunning = true;
    } catch (error) {
      LOG.error(`Error starting DailySurveyTracker: ${error}`);
      throw error;
    }
  }

  public async resume(): Promise<void> {
    LOG.info('Resuming DailySurveyTracker');
    this.isRunning = true;
    await this.processSurveys();
    this.startCheckJob();
  }

  public stop(): void {
    this.checkJob?.cancel();
    this.isRunning = false;
  }

  private startCheckJob(): void {
    this.checkJob?.cancel();
    this.checkJob = schedule.scheduleJob('* * * * *', () => this.processSurveys());
  }

  private async processSurveys(): Promise<void> {
    const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 });
    const now = new Date();

    for (const survey of this.surveys) {
      await this.processSurvey(survey, settings, now);
    }
  }

  private async processSurvey(
    survey: DailySurveyConfig,
    settings: Settings,
    now: Date
  ): Promise<void> {
    const samplingType = survey.samplingType;
    const invocationField = this.getInvocationField(samplingType);
    const pendingField = this.getPendingScheduledDateField(samplingType);
    const postponedUntilField = this.getPostponedUntilField(samplingType);
    let pendingScheduledDate = settings[pendingField];

    if (pendingScheduledDate && this.isMissedSurveyExpired(pendingScheduledDate, now)) {
      LOG.info(
        `Daily survey (${samplingType}) from ${pendingScheduledDate} is older than ${MAX_MISSED_SURVEY_AGE_DAYS} days and will not be shown`
      );
      settings[pendingField] = null;
      settings[postponedUntilField] = null;
      await settings.save();

      if (
        this.activeSurvey?.samplingType === samplingType &&
        this.activeSurvey.scheduledDate === pendingScheduledDate.getTime()
      ) {
        this.windowService.closeDailySurveyWindow(false, false);
        this.activeSurvey = null;
      }
      this.clearShownSurvey(samplingType, pendingScheduledDate);

      pendingScheduledDate = null;
    }

    const nextInvocation = settings[invocationField];
    const isPostponed = this.isSurveyPostponed(settings, samplingType, now);

    if (!nextInvocation) {
      await this.scheduleNextForSurvey(survey, settings, now);
    } else if (nextInvocation <= now && !isPostponed) {
      if (this.isMissedSurveyExpired(nextInvocation, now)) {
        LOG.info(
          `Daily survey (${samplingType}) due at ${nextInvocation} is older than ${MAX_MISSED_SURVEY_AGE_DAYS} days and will not be shown`
        );
        await this.scheduleNextForSurvey(survey, settings, now);
      } else {
        if (pendingScheduledDate && pendingScheduledDate.getTime() !== nextInvocation.getTime()) {
          LOG.info(
            `Replacing unanswered ${samplingType} daily survey from ${pendingScheduledDate} with the current survey`
          );
        }

        // Preserve the unanswered date while scheduling the next workday independently.
        settings[pendingField] = nextInvocation;
        settings[postponedUntilField] = null;
        await this.scheduleNextForSurvey(survey, settings, now);
        pendingScheduledDate = nextInvocation;
      }
    }

    if (pendingScheduledDate && !this.isSurveyPostponed(settings, samplingType, now)) {
      await this.showPendingSurvey(samplingType, pendingScheduledDate);
    }
  }

  private async showPendingSurvey(
    samplingType: DailySurveySamplingType,
    scheduledDate: Date
  ): Promise<void> {
    if (this.shownSurveyDates.get(samplingType) === scheduledDate.getTime()) {
      return;
    }

    LOG.info(`Daily survey (${samplingType}) was due at ${scheduledDate}, showing now`);
    await this.windowService.createDailySurveyWindow(samplingType, scheduledDate);
    this.shownSurveyDates.set(samplingType, scheduledDate.getTime());
    this.activeSurvey = { samplingType, scheduledDate: scheduledDate.getTime() };
  }

  private clearShownSurvey(samplingType: DailySurveySamplingType, scheduledDate: Date): void {
    if (this.shownSurveyDates.get(samplingType) === scheduledDate.getTime()) {
      this.shownSurveyDates.delete(samplingType);
    }
  }

  private isSurveyPostponed(
    settings: Settings,
    samplingType: DailySurveySamplingType,
    now: Date
  ): boolean {
    const postponedUntil = settings[this.getPostponedUntilField(samplingType)];
    return postponedUntil !== null && postponedUntil > now;
  }

  private async scheduleNextForSurvey(
    survey: DailySurveyConfig,
    settings?: Settings,
    now: Date = new Date()
  ): Promise<void> {
    const nextInvocation = await this.computeNextInvocation(survey, now);
    const settingsToUpdate =
      settings ?? (await Settings.findOneBy({ onlyOneEntityShouldExist: 1 }));

    settingsToUpdate[this.getInvocationField(survey.samplingType)] = nextInvocation;
    settingsToUpdate[this.getPostponedUntilField(survey.samplingType)] = null;

    await settingsToUpdate.save();
    LOG.info(`Next ${survey.samplingType} daily survey scheduled for ${nextInvocation}`);
  }

  private async computeNextInvocation(
    survey: DailySurveyConfig,
    now: Date = new Date()
  ): Promise<Date> {
    const schedule = await this.workScheduleService.getWorkSchedule();

    for (let dayOffset = 0; dayOffset <= 7; dayOffset++) {
      const candidate = new Date(now);
      candidate.setDate(candidate.getDate() + dayOffset);

      const dayIndex = (candidate.getDay() + 6) % 7;
      const dayName = weekDays[dayIndex];
      const workday = schedule[dayName];

      if (!workday.isWorking) continue;

      const timeStr = survey.samplingType === 'morning' ? workday.startTime : workday.endTime;
      const [hours, minutes] = timeStr.split(':').map(Number);

      const fireTime = new Date(candidate);
      fireTime.setHours(hours, minutes, 0, 0);
      fireTime.setMinutes(fireTime.getMinutes() + survey.delayInMinutes);

      if (fireTime > now) {
        return fireTime;
      }
    }

    // fallback: if all 7 days have isWorking=false (e.g. active work hours disabled),
    // schedule for tomorrow at 9am so the survey still fires on a default schedule
    LOG.warn('No working day found in the next 7 days, using fallback schedule (tomorrow 9am)');
    const fallback = new Date(now);
    fallback.setDate(fallback.getDate() + 1);
    fallback.setHours(9, 0, 0, 0);
    return fallback;
  }

  private getInvocationField(samplingType: DailySurveySamplingType): DailySurveyDateField {
    if (samplingType === 'morning') return 'nextDailySurveyMorningInvocation';
    if (samplingType === 'evening') return 'nextDailySurveyEveningInvocation';
    throw new Error(`Unknown samplingType: ${samplingType}`);
  }

  private getPendingScheduledDateField(
    samplingType: DailySurveySamplingType
  ): DailySurveyDateField {
    if (samplingType === 'morning') return 'pendingDailySurveyMorningScheduledDate';
    if (samplingType === 'evening') return 'pendingDailySurveyEveningScheduledDate';
    throw new Error(`Unknown samplingType: ${samplingType}`);
  }

  private getPostponedUntilField(samplingType: DailySurveySamplingType): DailySurveyDateField {
    if (samplingType === 'morning') return 'postponedDailySurveyMorningUntil';
    if (samplingType === 'evening') return 'postponedDailySurveyEveningUntil';
    throw new Error(`Unknown samplingType: ${samplingType}`);
  }

  private isMissedSurveyExpired(scheduledDate: Date, now: Date): boolean {
    return this.getLocalCalendarDayDifference(scheduledDate, now) > MAX_MISSED_SURVEY_AGE_DAYS;
  }

  private getLocalCalendarDayDifference(olderDate: Date, newerDate: Date): number {
    const localDayTimestamp = (date: Date): number =>
      Date.UTC(date.getFullYear(), date.getMonth(), date.getDate());

    return Math.floor((localDayTimestamp(newerDate) - localDayTimestamp(olderDate)) / 86_400_000);
  }

  private isBeforeToday(date: Date): boolean {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const compare = new Date(date);
    compare.setHours(0, 0, 0, 0);
    return compare < today;
  }

  public async complete(
    samplingType: DailySurveySamplingType,
    scheduledDate?: Date | null
  ): Promise<void> {
    if (!scheduledDate) {
      return;
    }

    const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 });
    const pendingField = this.getPendingScheduledDateField(samplingType);
    const pendingScheduledDate = settings[pendingField];
    if (!pendingScheduledDate || pendingScheduledDate.getTime() !== scheduledDate.getTime()) {
      return;
    }

    this.clearShownSurvey(samplingType, scheduledDate);
    if (
      this.activeSurvey?.samplingType === samplingType &&
      this.activeSurvey.scheduledDate === scheduledDate.getTime()
    ) {
      this.activeSurvey = null;
    }
    settings[pendingField] = null;
    settings[this.getPostponedUntilField(samplingType)] = null;
    await settings.save();
  }

  public async postpone(
    samplingType: DailySurveySamplingType,
    scheduledDate: Date | null,
    minutes: number
  ): Promise<boolean> {
    if (!scheduledDate) {
      return false;
    }

    const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 });
    const pendingScheduledDate = settings[this.getPendingScheduledDateField(samplingType)];
    if (
      !pendingScheduledDate ||
      pendingScheduledDate.getTime() !== scheduledDate.getTime() ||
      this.isBeforeToday(scheduledDate)
    ) {
      LOG.info(
        `Daily survey (${samplingType}) was scheduled on a previous day and cannot be postponed`
      );
      return false;
    }

    const newTime = new Date(Date.now() + minutes * 60 * 1000);

    // Store postponement separately so the original scheduled day remains available for late-survey messaging.
    settings[this.getPostponedUntilField(samplingType)] = newTime;
    this.clearShownSurvey(samplingType, scheduledDate);
    if (
      this.activeSurvey?.samplingType === samplingType &&
      this.activeSurvey.scheduledDate === scheduledDate.getTime()
    ) {
      this.activeSurvey = null;
    }

    await settings.save();
    LOG.info(`Daily survey (${samplingType}) postponed by ${minutes} minutes to ${newTime}`);
    return true;
  }
}
