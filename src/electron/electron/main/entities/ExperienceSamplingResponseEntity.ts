import { Column, Entity } from 'typeorm';
import BaseTrackedEntity from './BaseTrackedEntity';
import type { ExperienceSamplingAnswerType } from '../../../shared/StudyConfiguration';

@Entity({ name: 'experience_sampling_responses' })
export class ExperienceSamplingResponseEntity extends BaseTrackedEntity {
  @Column('datetime')
  promptedAt: Date;

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

  @Column('text', { default: 'auto' })
  trigger: 'manual' | 'auto';
}
