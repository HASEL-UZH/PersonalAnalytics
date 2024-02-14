import { Settings } from '../entities/Settings';
import studyConfig from '../../config/study.config';
import { generateAlphaNumericString } from './utils/helpers';

export class SettingsService {
  public async init(): Promise<void> {
    const isSettingsAlreadyCreated: boolean = await this.isSettingsAlreadyCreated();

    if (!isSettingsAlreadyCreated) {
      await this.createProhibitSubjectIdUpdateTrigger();
      await this.createDefaultSettings();
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
    await Settings.query(`
      create trigger onUpdateSettingsSubjectId before update of subjectId on settings
      begin
        select raise(ABORT, 'Updating settings.subjectId is prohibited');
      end;
    `);
  }
}
