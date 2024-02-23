import { Column, Entity } from 'typeorm';
import BaseTrackedEntity from './BaseTrackedEntity';

@Entity({ name: 'experience_sampling_responses' })
export class ExperienceSamplingResponseEntity extends BaseTrackedEntity {
  @Column('datetime')
  promptedAt: Date;

  @Column('text')
  question: string;

  @Column('text')
  responseOptions: string;

  @Column('int')
  scale: number;

  @Column('int', { nullable: true })
  response: number;

  @Column('boolean', { default: false, nullable: false })
  skipped: boolean;
}
