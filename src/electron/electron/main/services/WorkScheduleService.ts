import { WorkDayEntity } from '../entities/WorkDayEntity';
import studyConfig from '../../../shared/study.config';
import getMainLogger from '../../config/Logger';

const LOG = getMainLogger('WorkScheduleService');

interface Day {
  startTime: string;
  endTime: string;
  isWorking: boolean;
}

interface WorkSchedule {
  monday: Day, 
  tuesday: Day,
  wednesday: Day,
  thursday: Day,
  friday: Day,
  saturday: Day,
  sunday: Day
}

const defaultWorkSchedule = {
  monday: { startTime: "08:00", endTime: "17:00", isWorking: true },
  tuesday: { startTime: "08:00", endTime: "17:00", isWorking: true },
  wednesday: { startTime: "08:00", endTime: "17:00", isWorking: true },
  thursday: { startTime: "08:00", endTime: "17:00", isWorking: true },
  friday: { startTime: "08:00", endTime: "17:00", isWorking: true },
  saturday: { startTime: "08:00", endTime: "17:00", isWorking: false },
  sunday: { startTime: "08:00", endTime: "17:00", isWorking: false }
};

const weekDays = ["monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"];

export class WorkScheduleService {

  private schedulingService: any;

  async init() {
    if (studyConfig.trackers.taskTracker.enabled) {
      const { SchedulingService } = await import('@external/main/services/SchedulingService'); 
      const schedule = await this.getWorkSchedule()
      this.schedulingService = new SchedulingService(schedule);
    }
  }
  
  public async setWorkSchedule(schedule: WorkSchedule): Promise<void> {
    // clear existing work schedule
    await WorkDayEntity.delete({});

    for (let day of weekDays) {    
      await WorkDayEntity.create({
        day,
        startTime: schedule[day].startTime,
        endTime: schedule[day].endTime,
        isWorking: schedule[day].isWorking
      }).save();      
    }

    if (studyConfig.trackers.taskTracker.enabled) {
      this.schedulingService.updateWorkSchedule(schedule);
    } 
  }

  public async getWorkSchedule(): Promise<WorkSchedule> {
    let workdays = await WorkDayEntity.find();

    if (workdays.length === 0) {
      LOG.info("No work schedule found. Returning default work schedule.");
      return defaultWorkSchedule;
    }

    // Ensure all days are present in the work schedule
    if (workdays.length < 7 || new Set(workdays.map(day => day.day)).size < 7) {
      LOG.error("Work schedule is incomplete. Returning default work schedule.");
      return defaultWorkSchedule;
    }

    let schedule: WorkSchedule = {} as WorkSchedule;

    for (let day of weekDays) {
      const workday = workdays.find(d => d.day === day);
      if (workday) {
        schedule[day] = {
          startTime: workday.startTime,
          endTime: workday.endTime,
          isWorking: workday.isWorking
        };
      }
    }

    return schedule;
  }
}
