import { getMainLogger } from '../../config/Logger'
import * as schedule from 'node-schedule'
import { WindowService } from './WindowService'
import { Settings } from '../entities/Settings'
import studyConfig from '../../../shared/study.config'
import { WorkHoursDto, WorkHoursDayDto } from '../../../shared/dto/WorkHoursDto'

const LOG = getMainLogger('SchedulingService')

export class SchedulingService {
  private retrospectionJobs: schedule.Job[] = []
  private readonly windowService: WindowService

  constructor(windowService: WindowService, workSchedule: WorkHoursDto) {
    this.windowService = windowService
    LOG.info('Initializing SchedulingService with work schedule')
    this.updateRetrospectionJobs(workSchedule)

    // cleanup job daily at 4am
    schedule.scheduleJob('0 4 * * *', async (): Promise<void> => {
      LOG.info('Running 4am cleanup: closing retrospection window')
      this.windowService.closeRetrospectionWindow()
    })
  }

  public updateRetrospectionJobs(workSchedule: WorkHoursDto): void {
    // cancel existing retrospection jobs
    if (this.retrospectionJobs.length > 0) {
      this.retrospectionJobs.forEach(j => j.cancel())
    }
    this.retrospectionJobs = []

    if (!(studyConfig.enableRetrospection ?? true)) {
      LOG.info('Retrospection is disabled by researcher, skipping scheduling')
      return
    }

    // define new retrospection jobs
    const daysOfWeek: (keyof WorkHoursDto)[] = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday']
    daysOfWeek.forEach((day, index) => {
      const daySchedule: WorkHoursDayDto = workSchedule[day]
      if (daySchedule.isWorking) {
        const [endHour, endMinute] = daySchedule.endTime.split(':').map(Number)
        // schedule retrospection job for the end of the workday
        const job = schedule.scheduleJob(
          `${endMinute} ${endHour} * * ${index}`,
          async (fireDate: Date): Promise<void> => {
            LOG.info(`Retrospection Job was supposed to fire at ${fireDate}, fired at ${new Date()}`)
            await this.handleRetrospectionJob()
          }
        )
        if (job) {
          this.retrospectionJobs.push(job)
        }
      }
    })

    LOG.info(`Updated weekly retrospection jobs: ${this.retrospectionJobs.map(j => j.nextInvocation()).join(', ')}`)
  }

  private async handleRetrospectionJob(): Promise<void> {
    LOG.debug('handleRetrospectionJob called')

    // check if retrospection is disabled by researcher
    if (!(studyConfig.enableRetrospection ?? true)) {
      LOG.info('Retrospection is disabled by researcher, skipping')
      return
    }

    // check if retrospection is disabled by user
    const settings: Settings | null = await Settings.findOne({ where: { onlyOneEntityShouldExist: 1 } })
    if (!settings) {
      LOG.error('Settings not found')
      return
    }
    if (settings.userDisabledRetrospection === 1) {
      LOG.info('Retrospection is disabled by user, skipping')
      return
    }

    await this.windowService.createRetrospectionWindow()
  }
}
