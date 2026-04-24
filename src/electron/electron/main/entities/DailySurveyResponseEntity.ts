import { Column, Entity } from 'typeorm';
import BaseTrackedEntity from './BaseTrackedEntity';
import type { DailySurveySamplingType, ExperienceSamplingAnswerType } from '../../../shared/StudyConfiguration';

@Entity({ name: 'daily_survey_responses' })
export class DailySurveyResponseEntity extends BaseTrackedEntity {
  @Column('datetime')
  promptedAt: Date;

  @Column('text')
  samplingType: DailySurveySamplingType;

  @Column('text')
  question: string;

  @Column('text', { default: 'LikertScale' })
  answerType: ExperienceSamplingAnswerType;

  @Column('text', { nullable: true })
  responseOptions: string | null;

  @Column('int', { nullable: true })
  scale: number | null;

  @Column('text', { nullable: true })
  response: string | null;

  @Column('boolean', { default: false, nullable: false })
  skipped: boolean;
}
