import ActiveWindow from 'windows-activity-tracker/dist/types/ActiveWindow';
import { WindowActivityEntity } from '../../entities/WindowActivityEntity';

export class WindowActivityTrackerService {
  public static async handleWindowChange(window: ActiveWindow): Promise<void> {
    await WindowActivityEntity.create({
      windowTitle: window.windowTitle,
      processName: window.process,
      processPath: window.processPath,
      processId: window.processId,
      url: window.url,
      activity: window.activity,
      ts: window.ts
    }).save();
  }
}
