import * as schedule from 'node-schedule';
import { WindowService } from '../WindowService';
import { Tracker } from './Tracker';
import { getLogger } from '../../../shared/Logger';

const LOG = getLogger('ExperienceSamplingTracker');

export class ExperienceSamplingTracker implements Tracker {
  private experienceSamplingJob: schedule.Job;
  private readonly windowService: WindowService;
  private readonly intervalInMs: number;
  private readonly samplingRandomization: number;

  public readonly name: string = 'ExperienceSamplingTracker';
  public isRunning: boolean = false;

  constructor(windowService: WindowService, intervalInMs: number, samplingRandomization: number) {
    this.windowService = windowService;
    this.intervalInMs = intervalInMs;
    this.samplingRandomization = samplingRandomization;
    LOG.debug('SchedulingService constructor called');
  }
  public start(): void {
    try {
      this.experienceSamplingJob = schedule.scheduleJob(
        this.getRandomizedRecurrence(),
        (fireDate: Date): void => {
          LOG.info(
            `Experience Sampling Job was supposed to fire at ${fireDate}, fired at ${new Date()}`
          );
          this.handleExperienceSamplingJob();
        }
      );
      this.isRunning = true;
    } catch (error) {
      LOG.error(`Error starting experience sampling job: ${error}`);
      throw error;
    }
  }

  public stop(): void {
    this.experienceSamplingJob.cancel();
    this.isRunning = false;
  }

  private async handleExperienceSamplingJob(): Promise<void> {
    await this.windowService.createExperienceSamplingWindow();
    this.scheduleNextJob();
  }

  private scheduleNextJob(): void {
    this.experienceSamplingJob.reschedule(this.getRandomizedRecurrence());
  }

  private getRandomizedRecurrence(): string {
    const subtractOrAdd: 1 | -1 = Math.random() < 0.5 ? -1 : 1;
    const randomization: number = this.intervalInMs * this.samplingRandomization * subtractOrAdd;
    LOG.debug(
      `intervalInMs: ${this.intervalInMs}, samplingRandomization: ${this.samplingRandomization}, subtractOrAdd: ${subtractOrAdd}`
    );
    LOG.debug(`Randomization in minutes: ${randomization / 1000 / 60}`);
    const nextInvocation = new Date(Date.now() + this.intervalInMs + randomization);
    LOG.debug(`Next invocation: ${nextInvocation}`);
    return `${nextInvocation.getMinutes()} ${nextInvocation.getHours()} * * *`;
  }
}
