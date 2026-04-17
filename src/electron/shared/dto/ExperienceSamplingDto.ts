import type { ExperienceSamplingAnswerType } from '../StudyConfiguration';

export default interface ExperienceSamplingDto {
  id: string;
  question: string;
  answerType: ExperienceSamplingAnswerType;
  responseOptions: string | null;
  scale: number | null;
  response: string | null;
  skipped: boolean;
  trigger: 'manual' | 'auto';
  promptedAt: Date;
  createdAt: Date;
  updatedAt: Date;
  deletedAt: Date | null;
}
