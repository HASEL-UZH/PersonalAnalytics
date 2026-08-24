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
import ExperienceSamplingDto, {
  ExperienceSamplingResponseInput
} from '../../shared/dto/ExperienceSamplingDto';
import DailySurveyDto, { DailySurveyResponseInput } from '../../shared/dto/DailySurveyDto';
import { DailySurveyService } from '../main/services/DailySurveyService';
import { is } from '../main/services/utils/helpers';
import { JSDOM } from 'jsdom';
import DOMPurify from 'dompurify';
import { WorkScheduleService } from 'electron/main/services/WorkScheduleService';
import { WorkHoursDto } from 'shared/dto/WorkHoursDto';
import { getRetrospectionActivityDashboard } from '../main/services/RetrospectionService';
import type {
  RetrospectionDashboard,
  RetrospectionDataSection
} from '../../src/utils/retrospection/types';
import { SchedulingService } from '../main/services/SchedulingService';
import path from 'path';
import type {
  DailySurveySamplingType,
  ExperienceSamplingAnswerType
} from '../../shared/StudyConfiguration';
import { DailySurveyTracker } from '../main/services/trackers/DailySurveyTracker';
import { UsageDataService } from '../main/services/UsageDataService';
import { UsageDataEventType } from '../enums/UsageDataEventType.enum';
import { getRetrospectionTrackerAvailability } from '../../src/utils/retrospection/availability';
import {
  normalizeRetrospectionWorkdayRange,
  type RetrospectionWorkdayRangeInput
} from '../../shared/retrospection/Workday';

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
      createExperienceSamples: this.createExperienceSamples,
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
      retrospectionGetDashboard: this.retrospectionGetDashboard,
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
    skipped: boolean = false,
    trigger: 'manual' | 'auto' = 'auto'
  ) {
    await this.experienceSamplingService.createExperienceSample(
      promptedAt,
      question,
      answerType,
      responseOptions,
      scale,
      response,
      skipped,
      trigger
    );
  }

  private async createExperienceSamples(
    promptedAt: Date,
    responses: ExperienceSamplingResponseInput[],
    trigger: 'manual' | 'auto' = 'auto'
  ) {
    await this.experienceSamplingService.createExperienceSamples(promptedAt, responses, trigger);
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
      detail:
        "Your data will be uploaded via a secure, encrypted connection to a secure, encrypted store operated by the University of Zurich (Data Donation Lab). Your data will be processed in accordance with the study's consent form."
    });
    return response === 0;
  }

  private async showDataExportError(errorMessage?: string): Promise<void> {
    const message =
      `Please try again. If the export keeps failing, contact the study team (${studyConfig.contactName}, ${studyConfig.contactEmail}) and send them a screenshot of this error.` +
      (errorMessage ? `\n\nError message: ${errorMessage}` : '');
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

  private async retrospectionGetDashboard(
    workdayRange: RetrospectionWorkdayRangeInput
  ): Promise<RetrospectionDashboard> {
    const normalizedWorkdayRange = normalizeRetrospectionWorkdayRange(workdayRange);
    const trackerConfigs = await this.trackerService.getRetrospectionTrackerConfigs();
    const trackerAvailability = getRetrospectionTrackerAvailability(
      trackerConfigs.windowActivityMonitor,
      trackerConfigs.userInputMonitor,
      trackerConfigs.experienceSampling
    );
    const activityInsightsEnabled = trackerAvailability.activityInsightsEnabled;
    const selfReportsEnabled = trackerAvailability.selfReportsEnabled;
    const activityPromise = activityInsightsEnabled
      ? getRetrospectionActivityDashboard(normalizedWorkdayRange)
      : Promise.resolve({
          activities: [],
          activeHours: undefined,
          longestActivePeriod: undefined,
          topApps: [],
          topWebsites: [],
          topWindowTitles: [],
          errors: [] as RetrospectionDataSection[]
        });
    const selfReportsPromise = selfReportsEnabled
      ? this.experienceSamplingService
          .getExperienceSamplingDtosForDay(normalizedWorkdayRange)
          .then((selfReports) => ({ selfReports, errors: [] as RetrospectionDataSection[] }))
          .catch((error) => {
            LOG.error('Error loading retrospection self-reports', error);
            return { selfReports: [], errors: ['selfReports'] as RetrospectionDataSection[] };
          })
      : Promise.resolve({ selfReports: [], errors: [] as RetrospectionDataSection[] });

    const [activityDashboard, selfReportResult] = await Promise.all([
      activityPromise,
      selfReportsPromise
    ]);
    return {
      ...activityDashboard,
      selfReports: selfReportResult.selfReports,
      trackerAvailability,
      errors: [...activityDashboard.errors, ...selfReportResult.errors]
    };
  }

  private async openRetrospection(): Promise<void> {
    await this.windowService.focusOrCreateRetrospectionWindow();
  }

  private closeRetrospectionWindow(): void {
    this.windowService.closeRetrospectionWindow();
  }

  private async createDailySurveyResponses(
    promptedAt: Date,
    samplingType: DailySurveySamplingType,
    scheduledDate: Date | null,
    responses: DailySurveyResponseInput[]
  ): Promise<void> {
    await this.dailySurveyService.createDailySurveyResponses(promptedAt, samplingType, responses);
    if (this.dailySurveyTracker) {
      await this.dailySurveyTracker.complete(
        samplingType,
        scheduledDate ? new Date(scheduledDate) : null
      );
    }
  }

  private resizeDailySurveyWindow(height: number): void {
    this.windowService.resizeDailySurveyWindow(height);
  }

  private closeDailySurveyWindow(skipped: boolean): void {
    this.windowService.closeDailySurveyWindow(skipped);
  }

  private async postponeDailySurvey(
    samplingType: DailySurveySamplingType,
    scheduledDate: Date | null,
    minutes: number
  ): Promise<void> {
    if (this.dailySurveyTracker) {
      const wasPostponed = await this.dailySurveyTracker.postpone(
        samplingType,
        scheduledDate ? new Date(scheduledDate) : null,
        minutes
      );
      if (!wasPostponed) {
        return;
      }
    }
    UsageDataService.createNewUsageDataEvent(
      UsageDataEventType.DailySurveyPostponed,
      JSON.stringify({ samplingType, postponedMinutes: minutes })
    );
    this.windowService.closeDailySurveyWindow(false, false);
  }

  private async getMostRecentDailySurveyDtos(itemCount: number): Promise<DailySurveyDto[]> {
    return await this.dailySurveyService.getMostRecentDailySurveyDtos(itemCount);
  }
}
