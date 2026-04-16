import type { DailySurveySamplingType, ExperienceSamplingAnswerType } from '../StudyConfiguration';

export default interface DailySurveyDto {
  id: string;
  promptedAt: Date;
  samplingType: DailySurveySamplingType;
  question: string;
  answerType: ExperienceSamplingAnswerType;
  responseOptions: string | null;
  scale: number | null;
  response: string | null;
  skipped: boolean;
  createdAt: Date;
  updatedAt: Date;
  deletedAt: Date | null;
}
