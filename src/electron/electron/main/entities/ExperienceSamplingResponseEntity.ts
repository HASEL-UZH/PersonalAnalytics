import { Entity } from 'typeorm';
import BaseTrackedEntity from './BaseTrackedEntity';

@Entity({ name: 'experience_sampling_responses' })
export class WindowActivityEntity extends BaseTrackedEntity {}
