export interface WorkHoursDayDto {
  startTime: string;
  endTime: string;
  isWorking: boolean;
}

export interface WorkHoursDto {
  monday: WorkHoursDayDto, 
  tuesday: WorkHoursDayDto,
  wednesday: WorkHoursDayDto,
  thursday: WorkHoursDayDto,
  friday: WorkHoursDayDto,
  saturday: WorkHoursDayDto,
  sunday: WorkHoursDayDto
}
