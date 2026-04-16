import * as schedule from 'node-schedule';
import { WindowService } from '../WindowService';
import { Tracker } from './Tracker';
import getMainLogger from '../../../config/Logger';
import { Settings } from '../../entities/Settings';
import { WorkScheduleService } from '../WorkScheduleService';
import type { DailySurveyConfig, DailySurveySamplingType } from '../../../../shared/StudyConfiguration';

const LOG = getMainLogger('DailySurveyTracker');

const weekDays = ['monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday'];

export class DailySurveyTracker implements Tracker {
  private checkJob: schedule.Job;
  private readonly windowService: WindowService;
  private readonly workScheduleService: WorkScheduleService;
  private readonly surveys: DailySurveyConfig[];

  public readonly name: string = 'DailySurveyTracker';
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
        const invocationField = survey.samplingType === 'morning'
          ? 'nextDailySurveyMorningInvocation'
          : 'nextDailySurveyEveningInvocation';

        const nextInvocation = settings[invocationField];
        if (nextInvocation && nextInvocation <= now) {
          LOG.info(`Daily survey (${survey.samplingType}) is due`);
          await this.windowService.createDailySurveyWindow(survey.samplingType, nextInvocation);
          await this.scheduleNextForSurvey(survey);
        }
      }
    });
  }

  private async scheduleAllSurveys(): Promise<void> {
    for (const survey of this.surveys) {
      const invocationField = survey.samplingType === 'morning'
        ? 'nextDailySurveyMorningInvocation'
        : 'nextDailySurveyEveningInvocation';

      const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 });
      if (!settings[invocationField] || settings[invocationField] <= new Date()) {
        await this.scheduleNextForSurvey(survey);
      }
    }
  }

  private async scheduleNextForSurvey(survey: DailySurveyConfig): Promise<void> {
    const nextInvocation = await this.computeNextInvocation(survey);
    const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 });

    if (survey.samplingType === 'morning') {
      settings.nextDailySurveyMorningInvocation = nextInvocation;
    } else {
      settings.nextDailySurveyEveningInvocation = nextInvocation;
    }

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

    const fallback = new Date(now);
    fallback.setDate(fallback.getDate() + 1);
    fallback.setHours(9, 0, 0, 0);
    return fallback;
  }

  public async postpone(samplingType: DailySurveySamplingType, minutes: number): Promise<void> {
    const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 });
    const newTime = new Date(Date.now() + minutes * 60 * 1000);

    if (samplingType === 'morning') {
      settings.nextDailySurveyMorningInvocation = newTime;
    } else {
      settings.nextDailySurveyEveningInvocation = newTime;
    }

    await settings.save();
    LOG.info(`Daily survey (${samplingType}) postponed by ${minutes} minutes to ${newTime}`);
  }
}
