import { ExperienceSamplingService } from '../main/services/ExperienceSamplingService';
import { app, ipcMain, IpcMainInvokeEvent } from 'electron';
import { WindowService } from '../main/services/WindowService';
import { getLogger } from '../shared/Logger';
import { TypedIpcMain } from '../../src/utils/TypedIpcMain';
import Commands from '../../src/utils/Commands';
import Events from '../../src/utils/Events';
import StudyInfoDto from '../../shared/dto/StudyInfoDto';
import { Settings } from '../main/entities/Settings';
import studyConfig from '../../shared/study.config';
import { TrackerService } from '../main/services/trackers/TrackerService';

const LOG = getLogger('IpcHandler');

export class IpcHandler {
  private readonly actions: any;
  private readonly windowService: WindowService;
  private readonly trackerService: TrackerService;

  private readonly experienceSamplingService: ExperienceSamplingService;
  private typedIpcMain: TypedIpcMain<Events, Commands> = ipcMain as TypedIpcMain<Events, Commands>;

  constructor(
    windowService: WindowService,
    trackerService: TrackerService,
    experienceSamplingService: ExperienceSamplingService
  ) {
    this.windowService = windowService;
    this.trackerService = trackerService;
    this.experienceSamplingService = experienceSamplingService;
    this.actions = {
      createExperienceSample: this.createExperienceSample,
      closeExperienceSamplingWindow: this.closeExperienceSamplingWindow,
      getStudyInfo: this.getStudyInfo
    };
  }

  public init(): void {
    Object.keys(this.actions).forEach((action: string): void => {
      LOG.info(`ipcMain.handle setup: ${action}`);
      ipcMain.handle(action, async (_event: IpcMainInvokeEvent, ...args): Promise<any> => {
        try {
          return await this.actions[action].apply(this, args);
        } catch (error) {
          LOG.error(error);
          return error;
        }
      });
    });
  }

  private async createExperienceSample(
    promptedAt: number,
    question: string,
    response: number,
    skipped: boolean = false
  ) {
    await this.experienceSamplingService.createExperienceSample(
      promptedAt,
      question,
      response,
      skipped
    );
  }

  private closeExperienceSamplingWindow(): void {
    this.windowService.closeExperienceSamplingWindow();
  }

  private async getStudyInfo(): Promise<StudyInfoDto> {
    const settings: Settings = await Settings.findOne({ where: { onlyOneEntityShouldExist: 1 } });
    return {
      studyName: settings.studyName,
      subjectId: settings.subjectId,
      shortDescription: studyConfig.shortDescription,
      infoUrl: studyConfig.infoUrl,
      privacyPolicyUrl: studyConfig.privacyPolicyUrl,
      contactName: studyConfig.contactName,
      contactEmail: studyConfig.contactEmail,
      appVersion: app.getVersion(),
      currentlyActiveTrackers: this.trackerService.getRunningTrackerNames()
    };
  }
}
