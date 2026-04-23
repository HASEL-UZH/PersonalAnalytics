import { ExperienceSamplingService } from '../main/services/ExperienceSamplingService';
import { app, dialog, ipcMain, IpcMainInvokeEvent, shell, systemPreferences } from 'electron';
import { WindowService } from '../main/services/WindowService';
import { getMainLogger } from '../config/Logger';
import { TypedIpcMain } from '../../src/utils/TypedIpcMain';
import Commands from '../../src/utils/Commands';
import Events from '../../src/utils/Events';
import { DataExportType } from '../../shared/DataExportType.enum';
import { DataExportFormat } from '../../shared/DataExportFormat.enum';
import StudyInfoDto from '../../shared/dto/StudyInfoDto';
import { Settings } from '../main/entities/Settings';
import studyConfig from '../../shared/study.config';
import { TrackerService } from '../main/services/trackers/TrackerService';
import { WindowActivityTrackerService } from '../main/services/trackers/WindowActivityTrackerService';
import { UserInputTrackerService } from '../main/services/trackers/UserInputTrackerService';
import { DataExportService } from '../main/services/DataExportService';
import UserInputDto from '../../shared/dto/UserInputDto';
import WindowActivityDto from '../../shared/dto/WindowActivityDto';
import ExperienceSamplingDto from '../../shared/dto/ExperienceSamplingDto';
import DailySurveyDto from '../../shared/dto/DailySurveyDto';
import { DailySurveyService, DailySurveyResponseInput } from '../main/services/DailySurveyService';
import { is } from '../main/services/utils/helpers';
import { JSDOM } from 'jsdom';
import DOMPurify from 'dompurify';
import { WorkScheduleService } from 'electron/main/services/WorkScheduleService'
import { WorkHoursDto } from 'shared/dto/WorkHoursDto'
import { getActivitySessions, getAppUsageSessions, getLongestTimeActiveInsight, ActivitySessions, TimeActive } from '../main/services/RetrospectionService'
import { SchedulingService } from '../main/services/SchedulingService'
import path from 'path';
import type { DailySurveySamplingType, ExperienceSamplingAnswerType } from '../../shared/StudyConfiguration';
import { DailySurveyTracker } from '../main/services/trackers/DailySurveyTracker';
import { UsageDataService } from '../main/services/UsageDataService';
import { UsageDataEventType } from '../enums/UsageDataEventType.enum';

const LOG = getMainLogger('IpcHandler');

export class IpcHandler {
  private actions: any;
  private readonly windowService: WindowService;
  private readonly trackerService: TrackerService;

  private readonly experienceSamplingService: ExperienceSamplingService;
  private readonly dailySurveyService: DailySurveyService;
  private readonly windowActivityService: WindowActivityTrackerService;
  private readonly userInputService: UserInputTrackerService;
  private readonly dataExportService: DataExportService;
  private readonly workScheduleService: WorkScheduleService;
  private schedulingService: SchedulingService;
  private dailySurveyTracker: DailySurveyTracker | null = null;
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
    this.dailySurveyService = new DailySurveyService();
    this.windowActivityService = new WindowActivityTrackerService();
    this.userInputService = new UserInputTrackerService();
    this.dataExportService = new DataExportService();
    this.workScheduleService = workScheduleService;
  }

  public setDailySurveyTracker(tracker: DailySurveyTracker): void {
    this.dailySurveyTracker = tracker;
  }

  public setSchedulingService(schedulingService: SchedulingService): void {
    this.schedulingService = schedulingService;
  }

  public async init(): Promise<void> {
    this.actions = {
      openLogs: this.openLogs,
      openCollectedData: this.openCollected,
      getWorkHours: this.getWorkHours,
      setWorkHours: this.setWorkHours,
      setSettingsProp: this.setSettingsProp,
      getSettings: this.getSettings,
      createExperienceSample: this.createExperienceSample,
      resizeExperienceSamplingWindow: this.resizeExperienceSamplingWindow,
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
      showDataExportError: this.showDataExportError,
      confirmDDLUpload: this.confirmDDLUpload,
      startAllTrackers: this.startAllTrackers,
      triggerPermissionCheckAccessibility: this.triggerPermissionCheckAccessibility,
      triggerPermissionCheckScreenRecording: this.triggerPermissionCheckScreenRecording,
      retrospectionGetActivities: this.retrospectionGetActivities,
      retrospectionLoadLongestTimeActive: this.retrospectionLoadLongestTimeActive,
      retrospectionGetTopThreeMostActiveApps: this.retrospectionGetTopThreeMostActiveApps,
      openRetrospection: this.openRetrospection,
      closeRetrospectionWindow: this.closeRetrospectionWindow,
      createDailySurveyResponses: this.createDailySurveyResponses,
      resizeDailySurveyWindow: this.resizeDailySurveyWindow,
      closeDailySurveyWindow: this.closeDailySurveyWindow,
      postponeDailySurvey: this.postponeDailySurvey,
      getMostRecentDailySurveyDtos: this.getMostRecentDailySurveyDtos
    };

    Object.keys(this.actions).forEach((action: string): void => {
      LOG.info(`ipcMain.handle setup: ${action}`);
      ipcMain.handle(action, async (_event: IpcMainInvokeEvent, ...args): Promise<any> => {
        try {
          return await this.actions[action].apply(this, args);
        } catch (error) {
          LOG.error(error);
          // return error;
          throw error;
        }
      });
    });
  }

  private async createExperienceSample(
    promptedAt: Date,
    question: string,
    answerType: ExperienceSamplingAnswerType,
    responseOptions: string | null,
    scale: number | null,
    response?: string,
    skipped: boolean = false
  ) {
    await this.experienceSamplingService.createExperienceSample(
      promptedAt,
      question,
      answerType,
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

  private resizeExperienceSamplingWindow(height: number): void {
    this.windowService.resizeExperienceSamplingWindow(height);
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

    if (this.schedulingService) {
      this.schedulingService.updateRetrospectionJobs(schedule);
    }
  }

  private async setSettingsProp(prop: string, value: any): Promise<void> {
    const settings: Settings = await Settings.findOne({ where: { onlyOneEntityShouldExist: 1 } });
    settings[prop] = value;
    await settings.save();

    try {
      await this.windowService.updateTray();
    } catch (e) {
      LOG.warn('Failed to update tray after settings change', e);
    }
  }

  private async getSettings(): Promise<Settings> {
    const settings: Settings = await Settings.findOne({ where: { onlyOneEntityShouldExist: 1 } });
    if (!settings) {
      throw new Error('Settings not found');
    }
    return settings;
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
      currentlyActiveTrackers: this.trackerService.getRunningTrackerNames(),
      enabledWorkHours: settings.enabledWorkHours
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
    encryptData: boolean,
    exportFormat: DataExportFormat,
    exportDDLProjectName?: string
  ): Promise<{ fullPath: string; fileName: string }> {
    return this.dataExportService.startDataExport(
      windowActivityExportType,
      userInputExportType,
      obfuscationTerms,
      encryptData,
      exportFormat,
      exportDDLProjectName
    );
  }

  private async revealItemInFolder(path: string): Promise<void> {
    this.windowService.showItemInFolder(path);
  }
  
  private async openUploadUrl(): Promise<void> {
    this.windowService.openExternal();
  }

  private async confirmDDLUpload(): Promise<boolean> {
    const { response } = await dialog.showMessageBox({
      type: 'question',
      buttons: ['Yes', 'Cancel'],
      defaultId: 0,
      cancelId: 1,
      title: 'Confirm Data Donation',
      message: `Do you agree to donate and upload your data to the ${studyConfig.name} study?`,
      detail: 'Your data will be uploaded via a secure, encrypted connection to a secure, encrypted store operated by the University of Zurich (Data Donation Lab). Your data will be processed in accordance with the study\'s consent form.'
    });
    return response === 0;
  }

  private async showDataExportError(errorMessage?: string): Promise<void> {
    const message = `Please try again. If the export keeps failing, contact the study team (${studyConfig.contactName}, ${studyConfig.contactEmail}) and send them a screenshot of this error.` 
                      + (errorMessage ? `\n\nError message: ${errorMessage}` : '');
    dialog.showErrorBox('Study Data Export failed', message);
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

  private async retrospectionGetActivities(date: Date): Promise<ActivitySessions[]> {
    return await getActivitySessions(new Date(date));
  }

  private async retrospectionLoadLongestTimeActive(date: Date): Promise<TimeActive | undefined> {
    try {
      return await getLongestTimeActiveInsight(new Date(date));
    } catch (error) {
      LOG.error('Error loading longest time active', error);
    }
  }

  private async retrospectionGetTopThreeMostActiveApps(date: Date): Promise<ActivitySessions[] | undefined> {
    try {
      return (await getAppUsageSessions(new Date(date)))
        .sort((a, b) => b.totalDurationMs - a.totalDurationMs)
        .slice(0, 3);
    } catch (error) {
      LOG.error('Error loading top apps', error);
    }
  }

  private async openRetrospection(): Promise<void> {
    await this.windowService.createRetrospectionWindow();
  }

  private closeRetrospectionWindow(): void {
    this.windowService.closeRetrospectionWindow();
  }

  private async createDailySurveyResponses(
    promptedAt: Date,
    samplingType: DailySurveySamplingType,
    responses: DailySurveyResponseInput[]
  ): Promise<void> {
    await this.dailySurveyService.createDailySurveyResponses(promptedAt, samplingType, responses);
  }

  private resizeDailySurveyWindow(height: number): void {
    this.windowService.resizeDailySurveyWindow(height);
  }

  private closeDailySurveyWindow(skipped: boolean): void {
    this.windowService.closeDailySurveyWindow(skipped);
  }

  private async postponeDailySurvey(samplingType: DailySurveySamplingType, minutes: number): Promise<void> {
    UsageDataService.createNewUsageDataEvent(
      UsageDataEventType.DailySurveyPostponed,
      JSON.stringify({ samplingType, postponedMinutes: minutes })
    );
    if (this.dailySurveyTracker) {
      await this.dailySurveyTracker.postpone(samplingType, minutes);
    }
    this.windowService.closeDailySurveyWindow(false);
  }

  private async getMostRecentDailySurveyDtos(itemCount: number): Promise<DailySurveyDto[]> {
    return await this.dailySurveyService.getMostRecentDailySurveyDtos(itemCount);
  }
}
