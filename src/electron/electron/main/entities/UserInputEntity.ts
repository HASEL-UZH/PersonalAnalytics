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

  @Column('float', { nullable: false })
  scrollDelta: number;

  @Column('datetime', { nullable: false })
  tsStart: Date;

  @Column('datetime', { nullable: false })
  tsEnd: Date;
}
