import { WorkDayEntity } from '../entities/WorkDayEntity';
import studyConfig from '../../../shared/study.config';
import getMainLogger from '../../config/Logger';
import {WorkHoursDto}  from '../../../shared/dto/WorkHoursDto';

const LOG = getMainLogger('WorkScheduleService');

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

  async init() { }
  
  public async setWorkSchedule(schedule: WorkHoursDto): Promise<void> {
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

    if (studyConfig.trackers.taskTracker?.enabled) {
      this.schedulingService.updateWorkSchedule(schedule);
    } 
  }

  public async currentlyWithinWorkHours(): Promise<boolean> {
    const schedule =await this.getWorkSchedule()
    const now = new Date();
    const day = weekDays[(now.getDay() -1) % 7];
    const workday = schedule[day];
    const start = new Date();
    start.setHours(parseInt(workday.startTime.split(":")[0]), parseInt(workday.startTime.split(":")[1]), 0);
    const end = new Date();
    end.setHours(parseInt(workday.endTime.split(":")[0]), parseInt(workday.endTime.split(":")[1]), 0);
    return now >= start && now <= end;  
  }

  public async getWorkSchedule(): Promise<WorkHoursDto> {
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

    let schedule: WorkHoursDto = {} as WorkHoursDto;

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
