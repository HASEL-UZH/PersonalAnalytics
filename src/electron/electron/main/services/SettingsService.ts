import { Settings } from '../entities/Settings';
import { generateAlphaNumericString } from './utils/helpers';
import studyConfig from '../../../shared/study.config';
import { getLogger } from '../../shared/Logger';

const LOG = getLogger('SettingsService');

export class SettingsService {
  public async init(): Promise<void> {
    const isSettingsAlreadyCreated: boolean = await this.isSettingsAlreadyCreated();
    LOG.debug(`SettingsService.init: isSettingsAlreadyCreated=${isSettingsAlreadyCreated}`);

    if (!isSettingsAlreadyCreated) {
      LOG.info(`Creating default settings`);
      try {
        await this.createProhibitSubjectIdUpdateTrigger();
        await this.createDefaultSettings();
      } catch (error) {
        LOG.error(`Error while creating default settings: ${error}`);
      }
    }
  }

  private async createDefaultSettings(): Promise<void> {
    await Settings.create({
      subjectId: generateAlphaNumericString(studyConfig.subjectIdLength),
      studyName: studyConfig.name
    }).save();
  }

  private async isSettingsAlreadyCreated(): Promise<boolean> {
    const settings: Settings = await Settings.findOne({ where: { onlyOneEntityShouldExist: 1 } });
    return !!settings;
  }

  private async createProhibitSubjectIdUpdateTrigger(): Promise<void> {
    LOG.info(`Creating trigger onUpdateSettingsSubjectId`);
    await Settings.query(`
      create trigger onUpdateSettingsSubjectId before update of subjectId on settings
      begin
        select raise(ABORT, 'Updating settings.subjectId is prohibited');
      end;
    `);
    LOG.info(`Trigger onUpdateSettingsSubjectId created`);
  }
}
