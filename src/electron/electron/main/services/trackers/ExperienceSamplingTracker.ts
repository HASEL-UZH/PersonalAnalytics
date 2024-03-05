import * as schedule from 'node-schedule';
import { WindowService } from '../WindowService';
import { Tracker } from './Tracker';
import { getLogger } from '../../../shared/Logger';
import { Settings } from '../../entities/Settings';

const LOG = getLogger('ExperienceSamplingTracker');

export class ExperienceSamplingTracker implements Tracker {
  private checkIfExperienceSamplingIsDueJob: schedule.Job;
  private forcedExperienceSamplingJob: schedule.Job;
  private readonly windowService: WindowService;
  private readonly intervalInMs: number;
  private readonly samplingRandomization: number;

  public readonly name: string = 'ExperienceSamplingTracker';
  public isRunning: boolean = false;

  constructor(windowService: WindowService, intervalInMs: number, samplingRandomization: number) {
    this.windowService = windowService;
    this.intervalInMs = intervalInMs;
    this.samplingRandomization = samplingRandomization;
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
      const subtractOrAdd: 1 | -1 = Math.random() < 0.5 ? -1 : 1;
      const randomization =
        this.intervalInMs * this.samplingRandomization * Math.random() * subtractOrAdd;
      const nextInvocation = new Date(Date.now() + 30 * 60 * 1000 + randomization);

      this.forcedExperienceSamplingJob = schedule.scheduleJob(nextInvocation, async () => {
        await this.handleExperienceSamplingJob(nextInvocation);
      });
      LOG.info(`Resume, scheduled to fire at ${nextInvocation}`);
    } else {
      await this.startExperienceSamplingJob();
    }
    await this.startExperienceSamplingJob();
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
    this.checkIfExperienceSamplingIsDueJob.cancel();
    this.isRunning = false;
  }

  private async handleExperienceSamplingJob(fireDate: Date): Promise<void> {
    LOG.info(`Experience Sampling Job was supposed to fire at ${fireDate}, fired at ${new Date()}`);
    await this.windowService.createExperienceSamplingWindow();
    await this.scheduleNextJob();
  }

  private async scheduleNextJob(): Promise<void> {
    const nextInvocation: Date = this.getRandomNextInvocationDate();
    const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 });
    settings.nextExperienceSamplingInvocation = nextInvocation;
    await settings.save();
  }

  private getRandomNextInvocationDate(): Date {
    const subtractOrAdd: 1 | -1 = Math.random() < 0.5 ? -1 : 1;
    const randomization =
      this.intervalInMs * this.samplingRandomization * Math.random() * subtractOrAdd;
    LOG.debug(
      `intervalInMs: ${this.intervalInMs}, samplingRandomization: ${this.samplingRandomization}, subtractOrAdd: ${subtractOrAdd}`
    );
    LOG.debug(`Randomization: ${randomization} (${randomization / 1000 / 60} minutes)`);
    const nextInvocation = new Date(Date.now() + this.intervalInMs + randomization);
    LOG.debug(`Next invocation: ${nextInvocation}`);
    return nextInvocation;
  }
}
