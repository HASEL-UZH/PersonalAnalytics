import { LogFunctions } from 'electron-log';
import { getLogger } from '../../shared/Logger';
import * as schedule from 'node-schedule';
import { WindowService } from './WindowService';

const LOG: LogFunctions = getLogger('SchedulingService');

export class SchedulingService {
  private experienceSamplingJob: schedule.Job;
  private readonly windowService: WindowService;

  constructor(windowService: WindowService) {
    this.windowService = windowService;
  }
  public init() {
    LOG.silly('Initializing SchedulingService');
    this.experienceSamplingJob = schedule.scheduleJob(`0 * * * *`, (fireDate: Date): void => {
      LOG.info(`Experience Sampling Job was supposed to fire at ${fireDate}, fired at ${new Date()}`);
      this.handleExperienceSamplingJob();
    });

    LOG.info(
      `Next planned experience sampling job is at ${this.experienceSamplingJob.nextInvocation()}`
    );
  }

  private async handleExperienceSamplingJob(): Promise<void> {
    await this.windowService.createExperienceSamplingWindow();
  }
}
