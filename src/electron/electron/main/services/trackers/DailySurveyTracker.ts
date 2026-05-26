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

export class DailySurveyTracker implements Tracker {
  private checkJob: schedule.Job;
  private openSurveyKeys = new Set<DailySurveySamplingType>();
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
      await this.scheduleAllSurveys();
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
    this.startCheckJob();
  }

  public stop(): void {
    this.checkJob?.cancel();
    this.isRunning = false;
  }

  private startCheckJob(): void {
    this.checkJob?.cancel();
    this.checkJob = schedule.scheduleJob('* * * * *', async () => {
      const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 });
      const now = new Date();

      for (const survey of this.surveys) {
        const invocationField = this.getInvocationField(survey.samplingType);
        const postponedUntilField = this.getPostponedUntilField(survey.samplingType);
        const nextInvocation = settings[invocationField];
        const postponedUntil = settings[postponedUntilField];
        const isPostponed = postponedUntil && postponedUntil > now;
        if (
          nextInvocation &&
          nextInvocation <= now &&
          !isPostponed &&
          !this.openSurveyKeys.has(survey.samplingType)
        ) {
          LOG.info(`Daily survey (${survey.samplingType}) is due`);
          this.openSurveyKeys.add(survey.samplingType);
          await this.windowService.createDailySurveyWindow(survey.samplingType, nextInvocation);
        }
      }
    });
  }

  private async scheduleAllSurveys(): Promise<void> {
    for (const survey of this.surveys) {
      const invocationField = this.getInvocationField(survey.samplingType);
      const postponedUntilField = this.getPostponedUntilField(survey.samplingType);
      const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 });
      const now = new Date();
      const postponedUntil = settings[postponedUntilField];
      const isPostponed = postponedUntil && postponedUntil > now;
      if (
        settings[invocationField] &&
        settings[invocationField] <= now &&
        !isPostponed &&
        !this.openSurveyKeys.has(survey.samplingType)
      ) {
        LOG.info(
          `Daily survey (${survey.samplingType}) was due at ${settings[invocationField]}, showing now`
        );
        this.openSurveyKeys.add(survey.samplingType);
        // Keep nextInvocation unchanged until submit/skip so unattended app starts do not lose the original survey date.
        await this.windowService.createDailySurveyWindow(
          survey.samplingType,
          settings[invocationField]
        );
      } else if (!settings[invocationField]) {
        await this.scheduleNextForSurvey(survey);
      }
    }
  }

  private async scheduleNextForSurvey(survey: DailySurveyConfig): Promise<void> {
    const nextInvocation = await this.computeNextInvocation(survey);
    const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 });

    settings[this.getInvocationField(survey.samplingType)] = nextInvocation;
    settings[this.getPostponedUntilField(survey.samplingType)] = null;

    await settings.save();
    LOG.info(`Next ${survey.samplingType} daily survey scheduled for ${nextInvocation}`);
  }

  private async computeNextInvocation(survey: DailySurveyConfig): Promise<Date> {
    const schedule = await this.workScheduleService.getWorkSchedule();
    const now = new Date();

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

  private getInvocationField(
    samplingType: DailySurveySamplingType
  ): 'nextDailySurveyMorningInvocation' | 'nextDailySurveyEveningInvocation' {
    if (samplingType === 'morning') return 'nextDailySurveyMorningInvocation';
    if (samplingType === 'evening') return 'nextDailySurveyEveningInvocation';
    throw new Error(`Unknown samplingType: ${samplingType}`);
  }

  /**
   * Returns the Settings column that stores the postponed-until timestamp for the given daily survey type.
   */
  private getPostponedUntilField(
    samplingType: DailySurveySamplingType
  ): 'postponedDailySurveyMorningUntil' | 'postponedDailySurveyEveningUntil' {
    if (samplingType === 'morning') return 'postponedDailySurveyMorningUntil';
    if (samplingType === 'evening') return 'postponedDailySurveyEveningUntil';
    throw new Error(`Unknown samplingType: ${samplingType}`);
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
    this.openSurveyKeys.delete(samplingType);

    if (!scheduledDate) {
      return;
    }

    const survey = this.surveys.find((s) => s.samplingType === samplingType);
    if (!survey) {
      throw new Error(`Unknown daily survey samplingType: ${samplingType}`);
    }

    await this.scheduleNextForSurvey(survey);
  }

  public async postpone(samplingType: DailySurveySamplingType, minutes: number): Promise<boolean> {
    const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 });
    const scheduledDate = settings[this.getInvocationField(samplingType)];
    if (scheduledDate && this.isBeforeToday(scheduledDate)) {
      LOG.info(
        `Daily survey (${samplingType}) was scheduled on a previous day and cannot be postponed`
      );
      return false;
    }

    const newTime = new Date(Date.now() + minutes * 60 * 1000);

    // Store postponement separately so the original scheduled day remains available for late-survey messaging.
    settings[this.getPostponedUntilField(samplingType)] = newTime;
    this.openSurveyKeys.delete(samplingType);

    await settings.save();
    LOG.info(`Daily survey (${samplingType}) postponed by ${minutes} minutes to ${newTime}`);
    return true;
  }
}
