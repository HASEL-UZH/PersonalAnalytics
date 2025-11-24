import * as schedule from 'node-schedule';
import { WindowService } from '../WindowService';
import { Tracker } from './Tracker';
import getMainLogger from '../../../config/Logger';
import { Settings } from '../../entities/Settings';
import { powerMonitor } from 'electron';
import { WorkScheduleService } from '../WorkScheduleService'
import studyConfig from '../../../../shared/study.config'
import { DataSource } from 'typeorm';

const LOG = getMainLogger('ExperienceSamplingTracker');

export class ExperienceSamplingTracker implements Tracker {
  private checkIfExperienceSamplingIsDueJob: schedule.Job;
  private forcedExperienceSamplingJob: schedule.Job;
  private readonly windowService: WindowService;
  private readonly workScheduleService: WorkScheduleService;
  private readonly intervalInMs: number;
  private readonly samplingRandomization: number;

  public readonly name: string = 'ExperienceSamplingTracker';
  public isRunning: boolean = false;

  constructor(windowService: WindowService, workScheduleService: WorkScheduleService, intervalInMs: number, samplingRandomization: number) {
    this.windowService = windowService;
    this.workScheduleService = workScheduleService;
    this.intervalInMs = intervalInMs;
    this.samplingRandomization = samplingRandomization;
  }

  private async isUserDisabled(): Promise<boolean> {
    const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 });
    return (settings?.userDisabledExperienceSampling ?? 0) === 1;
  }

  private async getEffectiveIntervalMs(): Promise<number> {
    const allowChange = (studyConfig.trackers.experienceSamplingTracker.allowUserToChangeInterval ?? true) === true;
    if (allowChange) {
      const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 });
      const h = settings?.userDefinedExperienceSamplingInterval_h;
      if (h != null && Number.isFinite(h)) {
        return Math.max(1, h) * 60 * 60 * 1000; // hours → ms
      }
    }
    return this.intervalInMs;
  }

  public async start(): Promise<void> {
    try {
      await this.scheduleNextJob();
      await this.startExperienceSamplingJob();
      this.isRunning = true;
    } catch (error) {
      LOG.error(`Error starting experience sampling job: ${error}`);
      throw error;
    }
  }

  public async resume(): Promise<void> {
    LOG.info('Resuming ExperienceSamplingTracker');
    this.isRunning = true;
    const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 });
    // if the next invocation is in the past, we schedule the job in 30 + randomization minutes no matter what
    if (settings.nextExperienceSamplingInvocation <= new Date()) {
      LOG.info(
        'Next invocation is in the past, scheduling job to fire in 30 minutes + randomization'
      );
      await this.scheduleNextForcedExperienceSamplingJob();
    } else {
      await this.startExperienceSamplingJob();
    }
  }

  private async startExperienceSamplingJob(): Promise<void> {
    this.checkIfExperienceSamplingIsDueJob = schedule.scheduleJob('* * * * *', async () => {
      const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 });
      if (settings.nextExperienceSamplingInvocation <= new Date()) {
        LOG.info('Experience sampling is due, starting job');
        await this.handleExperienceSamplingJob(new Date());
      }
    });
  }

  public stop(): void {
    this.checkIfExperienceSamplingIsDueJob?.cancel();
    this.forcedExperienceSamplingJob?.cancel();
    this.isRunning = false;
  }

  private async handleExperienceSamplingJob(fireDate: Date): Promise<void> {
    LOG.info(`Experience Sampling Job was supposed to fire at ${fireDate}, fired at ${new Date()}`);

     const disabled = await this.isUserDisabled();
    if (disabled) {
      LOG.info('Experience sampling is disabled by user; not opening popup.');
      await this.scheduleNextJob();
      return;
    }

    // check if we can safely fire the experience sampling job
    // or have to consider work hours based on user settings and time/weekday
    const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 });  
    const userConsiderWorkHours = settings.enabledWorkHours;
    const inWorkHours = await this.workScheduleService.currentlyWithinWorkHours();
    const considerWorkHours = studyConfig.trackers.experienceSamplingTracker.enabledWorkHours;
    if (userConsiderWorkHours && considerWorkHours && !inWorkHours) {
        LOG.info('Currently outside of work hours, abort firing');
    } else {
      // within work hours; start experience sampling
      await this.windowService.createExperienceSamplingWindow();
    }
    // keep schedule for next experience sampling job no matter what..
    await this.scheduleNextJob();
  }

  private async scheduleNextJob(): Promise<void> {
    const nextInvocation: Date = await this.getRandomNextInvocationDate();
    const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 });
    settings.nextExperienceSamplingInvocation = nextInvocation;
    await settings.save();
  }

  private async scheduleNextForcedExperienceSamplingJob(): Promise<void> {
    const subtractOrAdd: 1 | -1 = Math.random() < 0.5 ? -1 : 1;
    const scheduleAfterResumeInMs = 30 * 60 * 1000;
    const randomization =
      scheduleAfterResumeInMs * this.samplingRandomization * Math.random() * subtractOrAdd;
    const nextInvocation = new Date(Date.now() + scheduleAfterResumeInMs + randomization);

    this.forcedExperienceSamplingJob = schedule.scheduleJob(nextInvocation, async () => {
      const systemIdleState: 'active' | 'idle' | 'locked' | 'unknown' =
        powerMonitor.getSystemIdleState(10 * 60);
      LOG.debug(
        `scheduleNextForcedExperienceSamplingJob(): System idle state: ${systemIdleState}, assuming idle after 10 minutes of inactivity`
      );

      const systemIdleTimeInSeconds: number = powerMonitor.getSystemIdleTime();
      LOG.debug(
        `scheduleNextForcedExperienceSamplingJob(): System idle time: ${systemIdleTimeInSeconds}`
      );
      if (systemIdleState !== 'active') {
        LOG.info(
          `scheduleNextForcedExperienceSamplingJob(): System idle time is greater than 30 minutes, not starting experience sampling job`
        );
        await this.scheduleNextForcedExperienceSamplingJob();
        return;
      }
      await this.handleExperienceSamplingJob(nextInvocation);
    });
    LOG.info(`scheduleNextForcedExperienceSamplingJob(): scheduled to fire at ${nextInvocation}`);

    await this.scheduleNextJob();
    await this.startExperienceSamplingJob();
  }

  private async getRandomNextInvocationDate(): Promise<Date> {
    const effectiveIntervalMs = await this.getEffectiveIntervalMs();
    const subtractOrAdd: 1 | -1 = Math.random() < 0.5 ? -1 : 1;
    const randomization =
      effectiveIntervalMs * this.samplingRandomization * Math.random() * subtractOrAdd;
    LOG.debug(
      `effectiveIntervalMs: ${effectiveIntervalMs}, samplingRandomization: ${this.samplingRandomization}, subtractOrAdd: ${subtractOrAdd}`
    );
    LOG.debug(`Randomization: ${randomization} (${randomization / 1000 / 60} minutes)`);
    const nextInvocation = new Date(Date.now() + effectiveIntervalMs + randomization);
    LOG.debug(`Next invocation: ${nextInvocation}`);
    return nextInvocation;
  }
}
