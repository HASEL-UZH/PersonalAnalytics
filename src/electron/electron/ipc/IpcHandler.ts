import { ExperienceSamplingService } from '../main/services/ExperienceSamplingService';
import { app, ipcMain, IpcMainInvokeEvent, shell, systemPreferences } from 'electron';
import { WindowService } from '../main/services/WindowService';
import { getMainLogger } from '../config/Logger';
import { TypedIpcMain } from '../../src/utils/TypedIpcMain';
import Commands from '../../src/utils/Commands';
import Events from '../../src/utils/Events';
import StudyInfoDto from '../../shared/dto/StudyInfoDto';
import { Settings } from '../main/entities/Settings';
import studyConfig from '../../shared/study.config';
import { TrackerService } from '../main/services/trackers/TrackerService';
import { WindowActivityTrackerService } from '../main/services/trackers/WindowActivityTrackerService';
import { UserInputTrackerService } from '../main/services/trackers/UserInputTrackerService';
import { DataExportType } from '../../shared/DataExportType.enum';
import { DataExportService } from '../main/services/DataExportService';
import UserInputDto from '../../shared/dto/UserInputDto';
import WindowActivityDto from '../../shared/dto/WindowActivityDto';
import ExperienceSamplingDto from '../../shared/dto/ExperienceSamplingDto';
import { is } from '../main/services/utils/helpers';
import { JSDOM } from 'jsdom';
import DOMPurify from 'dompurify';
import { WorkScheduleService } from 'electron/main/services/WorkScheduleService'
import { WorkHoursDto } from 'shared/dto/WorkHoursDto'
import path from 'path';

const LOG = getMainLogger('IpcHandler');

export class IpcHandler {
  private actions: any;
  private readonly windowService: WindowService;
  private readonly trackerService: TrackerService;

  private readonly experienceSamplingService: ExperienceSamplingService;
  private readonly windowActivityService: WindowActivityTrackerService;
  private readonly userInputService: UserInputTrackerService;
  private readonly dataExportService: DataExportService;
  private readonly workScheduleService: WorkScheduleService;
  private typedIpcMain: TypedIpcMain<Events, Commands> = ipcMain as TypedIpcMain<Events, Commands>;

  constructor(
    windowService: WindowService,
    trackerService: TrackerService,
    experienceSamplingService: ExperienceSamplingService,
    workScheduleService: WorkScheduleService
  ) {
    this.windowService = windowService;
    this.trackerService = trackerService;
    this.experienceSamplingService = experienceSamplingService;
    this.windowActivityService = new WindowActivityTrackerService();
    this.userInputService = new UserInputTrackerService();
    this.dataExportService = new DataExportService();
    this.workScheduleService = workScheduleService;
  }

  public async init(): Promise<void> {
    this.actions = {
      openLogs: this.openLogs,
      openCollectedData: this.openCollected,
      getWorkHours: this.getWorkHours,
      setWorkHours: this.setWorkHours,
      setWorkHoursEnabled: this.setWorkHoursEnabled,
      getWorkHoursEnabled: this.getWorkHoursEnabled,
      createExperienceSample: this.createExperienceSample,
      closeExperienceSamplingWindow: this.closeExperienceSamplingWindow,
      closeOnboardingWindow: this.closeOnboardingWindow,
      closeDataExportWindow: this.closeDataExportWindow,
      getStudyInfo: this.getStudyInfo,
      getMostRecentExperienceSamplingDtos: this.getMostRecentExperienceSamplingDtos,
      getMostRecentWindowActivityDtos: this.getMostRecentWindowActivityDtos,
      getMostRecentUserInputDtos: this.getMostRecentUserInputDtos,
      obfuscateWindowActivityDtosById: this.obfuscateWindowActivityDtosById,
      startDataExport: this.startDataExport,
      revealItemInFolder: this.revealItemInFolder,
      openUploadUrl: this.openUploadUrl,
      startAllTrackers: this.startAllTrackers,
      triggerPermissionCheckAccessibility: this.triggerPermissionCheckAccessibility,
      triggerPermissionCheckScreenRecording: this.triggerPermissionCheckScreenRecording
    };

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
    promptedAt: Date,
    question: string,
    responseOptions: string,
    scale: number,
    response: number,
    skipped: boolean = false
  ) {
    await this.experienceSamplingService.createExperienceSample(
      promptedAt,
      question,
      responseOptions,
      scale,
      response,
      skipped
    );
  }

  private openLogs() {
    LOG.info(`Opening logs at ${app.getPath('logs')}`);
    shell.openPath(`${app.getPath('logs')}`);
  }

  private openCollected() {
    LOG.info(`Opening collected data at ${app.getPath('userData')}`);
    shell.showItemInFolder(path.join(app.getPath('userData'), 'database.sqlite'));
  }

  private closeExperienceSamplingWindow(skippedExperienceSampling: boolean): void {
    this.windowService.closeExperienceSamplingWindow(skippedExperienceSampling);
  }

  private closeOnboardingWindow(): void {
    this.windowService.closeOnboardingWindow();
  }

  private closeDataExportWindow(): void {
    this.windowService.closeDataExportWindow();
  }
  
  private async getWorkHours(): Promise<WorkHoursDto> {
    return this.workScheduleService.getWorkSchedule();
  }

  private async setWorkHours(schedule: WorkHoursDto): Promise<void> {
    await this.workScheduleService.setWorkSchedule(schedule);
  }

  private async setWorkHoursEnabled(enabled: boolean): Promise<void> {
    const settings: Settings = await Settings.findOne({ where: { onlyOneEntityShouldExist: 1 } });
    settings.enabledWorkHours = enabled;
    await settings.save();
  }

  private async getWorkHoursEnabled(): Promise<boolean> {
    const settings: Settings = await Settings.findOne({ where: { onlyOneEntityShouldExist: 1 } });
    return settings.enabledWorkHours;
  }

  private async getStudyInfo(): Promise<StudyInfoDto> {
    const settings: Settings = await Settings.findOne({ where: { onlyOneEntityShouldExist: 1 } });

    const window = new JSDOM('').window;
    const purify = DOMPurify(window);

    const cleanDescription = purify.sanitize(studyConfig.shortDescription, {
      ALLOWED_TAGS: ['a', 'b', 'br', 'i', 'li', 'p', 'strong', 'u', 'ul'],
      ADD_ATTR: ['target']
    });

    return {
      studyName: settings.studyName,
      subjectId: settings.subjectId,
      shortDescription: cleanDescription,
      infoUrl: studyConfig.infoUrl,
      privacyPolicyUrl: studyConfig.privacyPolicyUrl,
      contactName: studyConfig.contactName,
      contactEmail: studyConfig.contactEmail,
      appVersion: app.getVersion(),
      currentlyActiveTrackers: this.trackerService.getRunningTrackerNames()
    };
  }

  private async getMostRecentExperienceSamplingDtos(
    itemCount: number
  ): Promise<ExperienceSamplingDto[]> {
    return await this.experienceSamplingService.getMostRecentExperienceSamplingDtos(itemCount);
  }

  private async getMostRecentWindowActivityDtos(itemCount: number): Promise<WindowActivityDto[]> {
    return await this.windowActivityService.getMostRecentWindowActivityDtos(itemCount);
  }

  private async obfuscateWindowActivityDtosById(ids: string[]): Promise<WindowActivityDto[]> {
    return await this.windowActivityService.obfuscateWindowActivityDtosById(ids);
  }

  private async getMostRecentUserInputDtos(itemCount: number): Promise<UserInputDto[]> {
    return await this.userInputService.getMostRecentUserInputDtos(itemCount);
  }

  private async startDataExport(
    windowActivityExportType: DataExportType,
    userInputExportType: DataExportType,
    obfuscationTerms: string[],
    encryptData: boolean
  ): Promise<string> {
    return this.dataExportService.startDataExport(
      windowActivityExportType,
      userInputExportType,
      obfuscationTerms,
      encryptData
    );
  }

  private async revealItemInFolder(path: string): Promise<void> {
    this.windowService.showItemInFolder(path);
  }
  
  private async openUploadUrl(): Promise<void> {
    this.windowService.openExternal();
  }

  private triggerPermissionCheckAccessibility(prompt: boolean): boolean {
    if (is.windows) {
      return true;
    }
    return systemPreferences.isTrustedAccessibilityClient(prompt);
  }

  private triggerPermissionCheckScreenRecording(): boolean {
    if (is.windows) {
      return true;
    }
    const status = systemPreferences.getMediaAccessStatus('screen');
    return status === 'granted';
  }

  private async startAllTrackers(): Promise<void> {
    try {
      await this.trackerService.startAllTrackers();
    } catch (e) {
      LOG.error('Error starting trackers', e);
    }
  }
}
