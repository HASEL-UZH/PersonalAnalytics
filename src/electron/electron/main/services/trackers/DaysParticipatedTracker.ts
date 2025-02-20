import * as schedule from 'node-schedule'
import { Tracker } from './Tracker'
import getMainLogger from '../../../config/Logger'
import { Settings } from '../../entities/Settings'
import studyConfig from '../../../../shared/study.config'
import { UserInputEntity } from '../../entities/UserInputEntity'
import { ExperienceSamplingResponseEntity } from '../../entities/ExperienceSamplingResponseEntity'

const LOG = getMainLogger('DaysParticipatedTracker')

export class DaysParticipatedTracker implements Tracker {
  private countParticipatedDaysJob: schedule.Job

  public readonly name: string = 'DaysParticipatedTracker';
  public isRunning: boolean = false;

  constructor() { }

  public async start(): Promise<void> {
    try {
      await this.updateDaysParticipated()
      await this.startCountingJob()
      this.isRunning = true
    } catch (error) {
      LOG.error(`Error starting days participated counting job: ${error}`)
      throw error
    }
  }

  private async updateDaysParticipated() {
    const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 })
    // as a simple default measure, we suggest to count as a study day if (a) at least one 
    // experience sampling response was given (if enabled) and (b) there are at 
    // least 60 user input entries (indicating 60+ minutes of activity)

    // get distinct days where count of rows larger than 60
    const resUI = await UserInputEntity.createQueryBuilder('userInput')
      .select('DATE(userInput.tsStart)', 'day')
      .groupBy('DATE(userInput.tsStart)')
      .having('COUNT(*) >= :minCount', { minCount: 60 })
      .getRawMany()

    if (!resUI) {
      LOG.info('No User Input found')
      await this.saveDaysParticipated(0)
      return
    }

    const daysUsage = new Set(resUI.map((res) => res.day))
    LOG.info(`Days Usage: ${Array.from(daysUsage)}`)

    if (!studyConfig.trackers.experienceSamplingTracker.enabled) {
      LOG.info('experience sampling tracker is disabled, counting "days participated" based on user input only.')
      await this.saveDaysParticipated(daysUsage.size)
      return
    }

    const resES = await ExperienceSamplingResponseEntity.createQueryBuilder('experienceSampling')
      .select('DISTINCT DATE(experienceSampling.promptedAt)', 'day')
      .getRawMany()

    if (!resES) {
      LOG.info('No Experience Sampling found')
      await this.saveDaysParticipated(0)
      return
    }

    const daysSamplingResponses = new Set(resES.map((res) => res.day))
    LOG.info(`Days Sampling: ${Array.from(daysSamplingResponses)}`)

    let count = 0
    daysUsage.forEach((day) => {
      if (daysSamplingResponses.has(day)) { count++ }
    })

    LOG.info(`Days Participated: ${count}`)
    await this.saveDaysParticipated(count)
  }

  private async saveDaysParticipated(days: number): Promise<void> {
    try {
      const settings: Settings = await Settings
        .findOneBy({ onlyOneEntityShouldExist: 1 })
      settings.daysParticipated = days
      await settings.save()
    } catch (error) {
      LOG.error(`Error saving days participated: ${error}`)
    }
  }

  public async resume(): Promise<void> {
    LOG.info('Resuming DaysParticipatedTracker')
    this.isRunning = true

    await this.updateDaysParticipated()
    await this.startCountingJob()
  }

  private async startCountingJob(): Promise<void> {
    // hourly at the 0th minute
    this.countParticipatedDaysJob = schedule.scheduleJob('0 * * * *', this.updateDaysParticipated)
  }

  public stop(): void {
    this.countParticipatedDaysJob?.cancel()
    this.isRunning = false
  }
}
