import { Column, Entity } from 'typeorm';
import BaseTrackedEntity from './BaseTrackedEntity';
import { UsageDataEventType } from '../../enums/UsageDataEventType.enum';

@Entity({ name: 'usage_data' })
export class UsageDataEntity extends BaseTrackedEntity {
  @Column({
    type: 'simple-enum',
    enum: UsageDataEventType
  })
  type: UsageDataEventType;

  @Column('text', { nullable: true })
  additionalInformation: string;
}
