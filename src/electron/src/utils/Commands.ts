import StudyInfoDto from '../../shared/dto/StudyInfoDto';
import { DataExportType } from '../../shared/DataExportType.enum';
import UserInputDto from '../../shared/dto/UserInputDto';
import WindowActivityDto from '../../shared/dto/WindowActivityDto';
import ExperienceSamplingDto from '../../shared/dto/ExperienceSamplingDto';

type Commands = {
  createExperienceSample: (
    promptedAt: Date,
    question: string,
    responseOptions: string,
    scale: number,
    response?: number,
    skipped?: boolean
  ) => Promise<void>;
  closeExperienceSamplingWindow: () => void;
  closeOnboardingWindow: () => void;
  closeDataExportWindow: () => void;
  getStudyInfo: () => Promise<StudyInfoDto>;
  getMostRecentExperienceSamplingDtos(itemCount: number): Promise<ExperienceSamplingDto[]>;
  getMostRecentUserInputDtos(itemCount: number): Promise<UserInputDto[]>;
  getMostRecentWindowActivityDtos(itemCount: number): Promise<WindowActivityDto[]>;
  obfuscateWindowActivityDtosById(ids: string[]): Promise<WindowActivityDto[]>;
  startDataExport: (
    windowActivityExportType: DataExportType,
    userInputExportType: DataExportType
  ) => Promise<void>;
  openExportFolder: () => Promise<void>;
  startAllTrackers: () => void;
  triggerPermissionCheckAccessibility: (prompt: boolean) => boolean;
  triggerPermissionCheckScreenRecording: () => boolean;
};
export default Commands;
