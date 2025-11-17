import { Column, Entity } from 'typeorm';
import BaseTrackedEntity from './BaseTrackedEntity';

@Entity({ name: 'user_input' })
export class UserInputEntity extends BaseTrackedEntity {
  @Column('int', { nullable: false })
  keysTotal: number;

  @Column('int', { nullable: false })
  clickTotal: number;

  @Column('float', { nullable: false })
  movedDistance: number;

  @Column('int', { nullable: false })
  scrollDelta: number;

  @Column('datetime', { nullable: false })
  tsStart: Date;

  @Column('datetime', { nullable: false })
  tsEnd: Date;

  @Column({ type: 'integer', nullable: true, default: 0 })
  keysLetter?: number;
  
  @Column({ type: 'integer', nullable: true, default: 0 })
  keysNumber?: number;

  @Column({ type: 'integer', nullable: true, default: 0 })
  keysNavigate?: number;

  @Column({ type: 'integer', nullable: true, default: 0 })
  keysDelete?: number;

  @Column({ type: 'integer', nullable: true, default: 0 })
  keysModifier?: number;

  @Column({ type: 'integer', nullable: true, default: 0 })
  keysSpace?: number;

  @Column({ type: 'integer', nullable: true, default: 0 })
  keysTab?: number;
  
  @Column({ type: 'integer', nullable: true, default: 0 })
  keyEnter?: number;

  @Column({ type: 'integer', nullable: true, default: 0 })
  keysOther?: number;
}
