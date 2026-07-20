import * as schedule from 'node-schedule';
import { Tracker } from './Tracker';
import { TrackerConfig } from '../../../types/StudyConfig';
import { TrackerType } from '../../../enums/TrackerType.enum';
import getMainLogger from '../../../config/Logger';
import { ExperienceSamplingTracker } from './ExperienceSamplingTracker';
import { WindowService } from '../WindowService';
import studyConfig from '../../../../shared/study.config';
import { UserInputEntity } from '../../entities/UserInputEntity';
import { MoreThanOrEqual } from 'typeorm';
import { WorkScheduleService } from '../WorkScheduleService';
import { DaysParticipatedTracker } from './DaysParticipatedTracker';
import { DailySurveyTracker } from './DailySurveyTracker';
import type { RetrospectionTrackerConfig } from '../../../../src/utils/retrospection/availability';

const LOG = getMainLogger('TrackerService');

export class TrackerService {
  private trackers: Tracker[] = [];
  private readonly config: TrackerConfig;
  private readonly windowService: WindowService;
  private readonly workScheduleService: WorkScheduleService;
  private checkIfUITIsWorkingJob: schedule.Job;
  private windowActivityTracker: Tracker | undefined;
  private userInputTracker: Tracker | undefined;
  private experienceSamplingTracker: ExperienceSamplingTracker | undefined;

  constructor(
    trackerConfig: TrackerConfig,
    windowService: WindowService,
    workScheduleService: WorkScheduleService
  ) {
    this.config = trackerConfig;
    this.windowService = windowService;
    this.workScheduleService = workScheduleService;
    LOG.debug(`TrackerService.constructor: config=${JSON.stringify(this.config)}`);
  }

  public async registerTrackerCallback(
    trackerType: TrackerType,
    callback?: (data: unknown) => void
  ): Promise<void> {
    if (this.isTrackerAlreadyRegistered(trackerType)) {
      throw new Error(`Tracker ${trackerType} already registered!`);
    }
    LOG.info(`Registering tracker ${trackerType}...`);

    if (
      this.config.windowActivityTracker.enabled &&
      trackerType === TrackerType.WindowsActivityTracker
    ) {
      // Used for getting URLs
      const accessibilityPermission: boolean = studyConfig.trackers.windowActivityTracker.trackUrls;
      // Used for getting window titles
      const screenRecordingPermission: boolean =
        studyConfig.trackers.windowActivityTracker.trackWindowTitles;
      const WAT = await import('windows-activity-tracker');
      const userInputTracker = new WAT.WindowsActivityTracker(
        callback,
        this.config.windowActivityTracker.intervalInMs,
        accessibilityPermission,
        screenRecordingPermission
      );
      this.trackers.push(userInputTracker);
      this.windowActivityTracker = userInputTracker;
    } else if (
      this.config.userInputTracker.enabled &&
      trackerType === TrackerType.UserInputTracker
    ) {
      const UIT = await import('user-input-tracker');
      const { intervalInMs, collectKeyDetails = false } = this.config.userInputTracker; // default to false for collectKeyDetails to avoid collecting potentially sensitive data if not explicitly enabled
      const userInputTracker = new UIT.UserInputTracker(callback, intervalInMs, collectKeyDetails);
      this.trackers.push(userInputTracker);
      this.userInputTracker = userInputTracker;
    } else if (
      this.config.experienceSamplingTracker.enabled &&
      trackerType === TrackerType.ExperienceSamplingTracker
    ) {
      const experienceSamplingTracker: ExperienceSamplingTracker = new ExperienceSamplingTracker(
        this.windowService,
        this.workScheduleService,
        this.config.experienceSamplingTracker.intervalInMs,
        this.config.experienceSamplingTracker.samplingRandomization
      );
      this.trackers.push(experienceSamplingTracker);
      this.experienceSamplingTracker = experienceSamplingTracker;
    } else if (trackerType === TrackerType.DaysParticipatedTracker) {
      const daysParticipatedTracker = new DaysParticipatedTracker();
      this.trackers.push(daysParticipatedTracker);
    } else if (
      this.config.dailySurveyTracker?.enabled &&
      trackerType === TrackerType.DailySurveyTracker
    ) {
      const dailySurveyTracker = new DailySurveyTracker(
        this.windowService,
        this.workScheduleService,
        this.config.dailySurveyTracker.surveys
      );
      this.trackers.push(dailySurveyTracker);
    } else {
      throw new Error(`Tracker ${trackerType} not enabled or unsupported!`);
    }
  }

  private setCheckIfUITIsWorkingJob(): void {
    if (!this.config.userInputTracker.enabled) {
      return;
    }
    if (this.checkIfUITIsWorkingJob) {
      this.checkIfUITIsWorkingJob.cancel();
      this.checkIfUITIsWorkingJob = null;
    }
    const uitIntervalInMs = this.config.userInputTracker.intervalInMs;
    const bufferInMs = 5000;
    const nextInvocationIn = new Date(Date.now() + uitIntervalInMs + bufferInMs);

    this.checkIfUITIsWorkingJob = schedule.scheduleJob(nextInvocationIn, async () => {
      const recentUserInputExists: boolean = await this.userInputDataExistsFromMsAgo(
        uitIntervalInMs + bufferInMs
      );
      if (!recentUserInputExists) {
        LOG.warn(
          `No user input data found from the last ${uitIntervalInMs + bufferInMs}ms. Was supposed to find one created at or after ${new Date(Date.now() - (uitIntervalInMs + bufferInMs))}`
        );
      }
      this.setCheckIfUITIsWorkingJob();
    });
  }

  private async userInputDataExistsFromMsAgo(msAgo: number): Promise<boolean> {
    const userInputEntity = await UserInputEntity.findOneBy({
      createdAt: MoreThanOrEqual(new Date(Date.now() - msAgo))
    });
    return userInputEntity !== null;
  }

  public async startAllTrackers() {
    await Promise.all(
      this.trackers.filter((t: Tracker) => !t.isRunning).map((t: Tracker) => t.start())
    );
    this.setCheckIfUITIsWorkingJob();
  }

  public async resumeAllTrackers(): Promise<void> {
    await Promise.all(
      this.trackers
        .filter((t: Tracker) => !t.isRunning)
        .map((t: Tracker): void => (t.resume ? t.resume() : t.start()))
    );
  }

  public async stopAllTrackers() {
    await Promise.all(
      this.trackers.filter((t: Tracker) => t.isRunning).map((t: Tracker) => t.stop())
    );
    if (this.config.userInputTracker.enabled) {
      this.checkIfUITIsWorkingJob?.cancel();
      this.checkIfUITIsWorkingJob = null;
    }
  }

  public getRunningTrackerNames() {
    return this.trackers.filter((t: Tracker) => t.isRunning).map((t: Tracker) => t.name);
  }

  public isAnyTrackerRunning() {
    return this.trackers.some((t: Tracker) => t.isRunning);
  }

  public getDailySurveyTracker(): DailySurveyTracker | undefined {
    return this.trackers.find((t) => t instanceof DailySurveyTracker) as DailySurveyTracker | undefined;
  }

  /**
   * Returns labels from the tracker implementations rather than duplicating them in study config.
   * Disabled trackers are created temporarily but never started, solely to read their display name.
   */
  public async getRetrospectionTrackerConfigs(): Promise<{
    windowActivityMonitor: RetrospectionTrackerConfig;
    userInputMonitor: RetrospectionTrackerConfig;
    experienceSampling: RetrospectionTrackerConfig;
  }> {
    const [windowActivityName, userInputName, experienceSamplingName] = await Promise.all([
      this.getWindowActivityTrackerName(),
      this.getUserInputTrackerName(),
      this.getExperienceSamplingTrackerName()
    ]);

    return {
      windowActivityMonitor: {
        name: windowActivityName,
        enabled: this.config.windowActivityTracker.enabled
      },
      userInputMonitor: { name: userInputName, enabled: this.config.userInputTracker.enabled },
      experienceSampling: {
        name: experienceSamplingName,
        enabled: this.config.experienceSamplingTracker.enabled
      }
    };
  }

  private async getWindowActivityTrackerName(): Promise<string> {
    if (this.windowActivityTracker) {
      return this.windowActivityTracker.name;
    }
    const WAT = await import('windows-activity-tracker');
    return new WAT.WindowsActivityTracker(() => undefined).name;
  }

  private async getUserInputTrackerName(): Promise<string> {
    if (this.userInputTracker) {
      return this.userInputTracker.name;
    }
    const UIT = await import('user-input-tracker');
    const tracker = new UIT.UserInputTracker(
      () => undefined,
      this.config.userInputTracker.intervalInMs,
      false
    );
    try {
      return tracker.name;
    } finally {
      tracker.terminate();
    }
  }

  private getExperienceSamplingTrackerName(): string {
    if (this.experienceSamplingTracker) {
      return this.experienceSamplingTracker.name;
    }
    return new ExperienceSamplingTracker(
      this.windowService,
      this.workScheduleService,
      this.config.experienceSamplingTracker.intervalInMs,
      this.config.experienceSamplingTracker.samplingRandomization
    ).name;
  }

  private isTrackerAlreadyRegistered(trackerType: TrackerType) {
    if (trackerType === TrackerType.DailySurveyTracker) {
      return this.getDailySurveyTracker() !== undefined;
    }
    return this.trackers.some((t: Tracker) => t.name === trackerType);
  }
}
