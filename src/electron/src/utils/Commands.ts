import StudyInfoDto from '../../shared/dto/StudyInfoDto';
import { WindowActivityEntity } from '../../electron/main/entities/WindowActivityEntity';
import { UserInputEntity } from '../../electron/main/entities/UserInputEntity';

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
  getStudyInfo: () => Promise<StudyInfoDto>;
  getMostRecentUserInputs(itemCount: number): Promise<UserInputEntity[]>;
  getMostRecentWindowActivities(itemCount: number): Promise<WindowActivityEntity[]>;
  obfuscateWindowActivitiesById(ids: string[]): Promise<WindowActivityEntity[]>;
  openExportFolder: () => Promise<void>;
  startAllTrackers: () => void;
  triggerPermissionCheckAccessibility: (prompt: boolean) => boolean;
  triggerPermissionCheckScreenRecording: () => boolean;
};
export default Commands;
