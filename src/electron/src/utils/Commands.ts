import StudyInfoDto from '../../shared/dto/StudyInfoDto';

type Commands = {
  createExperienceSample: (
    promptedAt: number,
    question: string,
    response?: number,
    skipped?: boolean
  ) => Promise<void>;
  closeExperienceSamplingWindow: () => void;
  closeOnboardingWindow: () => void;
  getStudyInfo: () => Promise<StudyInfoDto>;
  startAllTrackers: () => void;
};
export default Commands;
