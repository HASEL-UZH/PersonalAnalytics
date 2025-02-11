import { Entity, Column, BaseEntity, PrimaryGeneratedColumn } from 'typeorm';

@Entity({ name: 'work_schedule' })
export class WorkDayEntity extends BaseEntity  {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Column( { type: 'text', name: 'day' })
  day: string;

  @Column({ type: 'text', name: 'start_time' })
  startTime: string;

  @Column({ type: 'text', name: 'end_time' })
  endTime: string;

  @Column({ type: 'boolean', name: 'is_working' })
  isWorking: boolean;
}

