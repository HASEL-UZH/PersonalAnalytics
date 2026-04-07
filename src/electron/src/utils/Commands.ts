import StudyInfoDto from '../../shared/dto/StudyInfoDto';
import { DataExportType } from '../../shared/DataExportType.enum';
import { DataExportFormat } from '../../shared/DataExportFormat.enum';
import UserInputDto from '../../shared/dto/UserInputDto';
import WindowActivityDto from '../../shared/dto/WindowActivityDto';
import ExperienceSamplingDto from '../../shared/dto/ExperienceSamplingDto';
import { WorkHoursDto } from '../../shared/dto/WorkHoursDto'
import { Settings } from 'electron/main'
import type { ExperienceSamplingAnswerType } from '../../shared/StudyConfiguration';

type Commands = {
  createExperienceSample: (
    promptedAt: Date,
    question: string,
    answerType: ExperienceSamplingAnswerType,
    responseOptions: string | null,
    scale?: number | null,
    response?: string,
    skipped?: boolean
  ) => Promise<void>;
  resizeExperienceSamplingWindow: (height: number) => void;
  closeExperienceSamplingWindow: (skippedExperienceSampling: boolean) => void;
  closeOnboardingWindow: () => void;
  closeDataExportWindow: () => void;
  getStudyInfo: () => Promise<StudyInfoDto>;
  getWorkHours: () => Promise<WorkHoursDto>;
  setWorkHours: (schedule: WorkHoursDto) => Promise<void>;
  setSettingsProp: (prop: string, value: any) => Promise<void>;
  getSettings: () => Promise<Settings>;
  getWorkHoursEnabled: () => Promise<boolean>;
  openLogs: () => void;
  openCollectedData: () => void;
  getMostRecentExperienceSamplingDtos(itemCount: number): Promise<ExperienceSamplingDto[]>;
  getMostRecentUserInputDtos(itemCount: number): Promise<UserInputDto[]>;
  getMostRecentWindowActivityDtos(itemCount: number): Promise<WindowActivityDto[]>;
  obfuscateWindowActivityDtosById(ids: string[]): Promise<WindowActivityDto[]>;
  startDataExport: (
    windowActivityExportType: DataExportType,
    userInputExportType: DataExportType,
    obfuscationTerms: string[],
    encryptData: boolean,
    exportFormat: DataExportFormat,
    exportDdlProjectName?: string
  ) => Promise<{ fullPath: string; fileName: string }>;
  revealItemInFolder: (path: string) => Promise<void>;
  openUploadUrl: () => void;
  showDataExportError: (errorMessage?: string) => void;
  confirmDDLUpload: () => Promise<boolean>;
  startAllTrackers: () => void;
  triggerPermissionCheckAccessibility: (prompt: boolean) => boolean;
  triggerPermissionCheckScreenRecording: () => boolean;
  retrospectionGetActivities: (date: Date) => Promise<any[]>;
  retrospectionLoadLongestTimeActive: (date: Date) => Promise<any>;
  retrospectionGetTopThreeMostActiveApps: (date: Date) => Promise<any[]>;
  openRetrospection: () => Promise<void>;
  closeRetrospectionWindow: () => void;
};
export default Commands;
