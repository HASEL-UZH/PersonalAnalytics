import {
  BaseEntity,
  Check,
  Column,
  CreateDateColumn,
  Entity,
  PrimaryColumn,
  UpdateDateColumn
} from 'typeorm';

@Entity({ name: 'settings' })
@Check('onlyOneEntityShouldExist = 1')
export class Settings extends BaseEntity {
  // This is a hack to ensure that only one entity of this type exists
  // Trying to save a new entity will fail with a unique constraint violation
  @PrimaryColumn({ update: false, type: 'int', default: (): number => 1, nullable: false })
  readonly onlyOneEntityShouldExist: number = 1;

  @Column({ type: 'text', nullable: false })
  studyName: string;

  // This should not be updated after the initial creation
  // The SettingsService creates a trigger to prevent this
  @Column({
    type: 'text',
    nullable: false,
    update: false
  })
  subjectId: string;

  @Column({ type: 'boolean', nullable: false, default: true })
  enabledWorkHours: boolean;

  @Column({ type: 'boolean', nullable: false, default: false })
  onboardingShown: boolean;

  @Column({ type: 'boolean', nullable: false, default: false })
  studyAndTrackersStartedShown: boolean;

  @Column({ type: 'int', nullable: false, default: 0 })
  daysParticipated: number;

  @Column('datetime', { nullable: true })
  nextExperienceSamplingInvocation: Date;

  @CreateDateColumn({ name: 'created_at' })
  createdAt: Date;

  @UpdateDateColumn({ name: 'updated_at' })
  updatedAt: Date;
}
