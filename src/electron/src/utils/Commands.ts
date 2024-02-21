import { StudyInfoDto } from '../../shared/dto/StudyInfoDto';

type Commands = {
  createExperienceSample: (
    promptedAt: number,
    question: string,
    response?: number,
    skipped?: boolean
  ) => Promise<void>;
  closeExperienceSamplingWindow: () => Promise<void>;
  getStudyInfo: () => Promise<StudyInfoDto>;
};
export default Commands;
