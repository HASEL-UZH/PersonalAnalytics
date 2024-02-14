import {
  BaseEntity,
  Check,
  Column,
  CreateDateColumn,
  Entity,
  PrimaryGeneratedColumn,
  UpdateDateColumn
} from 'typeorm';

@Entity({ name: 'settings' })
@Check('onlyOneEntityShouldExist = 1')
export class Settings extends BaseEntity {
  // This is a hack to ensure that only one entity of this type exists
  // Trying to save a new entity will fail with a unique constraint violation
  @PrimaryGeneratedColumn({ update: false, type: 'int', default: () => '1', nullable: false })
  public onlyOneEntityShouldExist: number = 1;

  @Column({ type: 'text', nullable: false })
  studyName: string;

  // This should not be updated after the initial creation
  // The SettingsService creates a trigger to prevent this
  @Column({
    type: 'text',
    nullable: false,
    update: false
  })
  userId: string;

  @CreateDateColumn({ name: 'created_at' })
  createdAt: Date;

  @UpdateDateColumn({ name: 'updated_at' })
  updatedAt: Date;
}
