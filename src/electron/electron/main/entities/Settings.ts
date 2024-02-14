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
  @PrimaryGeneratedColumn({ type: 'int', default: () => '1', nullable: false })
  public onlyOneEntityShouldExist: 1;

  @Column({ type: 'text', nullable: false })
  studyName: string;

  @Column({ type: 'text', nullable: false })
  userId: string;

  @CreateDateColumn({ name: 'created_at' })
  createdAt: Date;

  @UpdateDateColumn({ name: 'updated_at' })
  updatedAt: Date;
}
