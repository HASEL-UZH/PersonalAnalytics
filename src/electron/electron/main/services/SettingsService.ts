import { Settings } from '../entities/Settings';
import studyConfig from '../../config/study.config';
import { generateAlphaNumericString } from './utils/helpers';

export class SettingsService {
  public async init(): Promise<void> {
    const isSettingsAlreadyCreated: boolean = await this.isSettingsAlreadyCreated();

    if (!isSettingsAlreadyCreated) {
      await this.createProhibitUserIdUpdateTrigger();
      await this.createDefaultSettings();
    }
  }

  private async createDefaultSettings(): Promise<void> {
    await Settings.create({
      userId: generateAlphaNumericString(studyConfig.userIdLength),
      studyName: studyConfig.name
    }).save();
  }

  private async isSettingsAlreadyCreated(): Promise<boolean> {
    const settings: Settings = await Settings.findOne({ where: { onlyOneEntityShouldExist: 1 } });
    return !!settings;
  }

  private async createProhibitUserIdUpdateTrigger(): Promise<void> {
    await Settings.query(`
      create trigger onUpdateSettingsUserId before update of userId on settings
      begin
        select raise(ABORT, 'Updating settings.userId is prohibited');
      end;
    `);
  }
}
