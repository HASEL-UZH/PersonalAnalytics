import { Entity } from 'typeorm';
import BaseTrackedEntity from './BaseTrackedEntity';

@Entity({ name: 'user_input' })
export class UserInputEntity extends BaseTrackedEntity {}
