import { TrackerConfig } from '../../types/StudyConfig';
import { TrackerType } from '../../enums/TrackerType.enum';
import { Tracker } from './trackers/Tracker';

export class TrackerService {
  private trackers: Tracker[] = [];
  private config: TrackerConfig;

  constructor(trackerConfig: TrackerConfig) {
    this.config = trackerConfig;
  }

  public async registerTrackerCallback(
    trackerType: TrackerType,
    callback: (data: unknown) => void
  ): Promise<void> {
    if (this.isTrackerAlreadyRegistered(trackerType)) {
      throw new Error(`Tracker ${trackerType} already registered!`);
    }
    if (
      this.config.windowActivityTracker.enabled &&
      trackerType === TrackerType.WindowsActivityTracker
    ) {
      const WAT = await import('windows-activity-tracker');
      const userInputTracker = new WAT.WindowsActivityTracker(
        callback,
        this.config.windowActivityTracker.checkingForWindowChangeIntervalInMs
      );
      this.trackers.push(userInputTracker);
    } else {
      throw new Error(`Tracker ${trackerType} not enabled or unsupported!`);
    }
  }

  public async startAllTrackers() {
    await Promise.all(this.trackers.map((t: Tracker) => t.start()));
  }

  public async stopAllTrackers() {
    await Promise.all(this.trackers.map((t: Tracker) => t.stop()));
  }

  public async terminateAllTrackers() {
    await Promise.all(this.trackers.map((t: Tracker) => t.terminate()));
  }

  public getRunningTrackerNames() {
    return this.trackers.filter((t: Tracker) => t.isRunning).map((t: Tracker) => t.name);
  }

  public isAnyTrackerRunning() {
    return this.trackers.some((t: Tracker) => t.isRunning);
  }

  private isTrackerAlreadyRegistered(trackerType: TrackerType) {
    return this.trackers.some((t: Tracker) => t.name === trackerType);
  }
}
