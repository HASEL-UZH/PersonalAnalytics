import type { DailySurveySamplingType, ExperienceSamplingAnswerType } from '../StudyConfiguration';

export interface DailySurveyResponseInput {
  question: string;
  answerType: ExperienceSamplingAnswerType;
  responseOptions: string | null;
  response: string | null;
  skipped: boolean;
}

export default interface DailySurveyDto {
  id: string;
  promptedAt: Date;
  samplingType: DailySurveySamplingType;
  question: string;
  answerType: ExperienceSamplingAnswerType;
  responseOptions: string | null;
  response: string | null;
  skipped: boolean;
  createdAt: Date;
  updatedAt: Date;
  deletedAt: Date | null;
}
