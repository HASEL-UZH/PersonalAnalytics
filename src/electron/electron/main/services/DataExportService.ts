import { DataExportType } from '../../../shared/DataExportType.enum';
import { getLogger } from '../../shared/Logger';
import path from 'path';
import { app } from 'electron';
import { is } from './utils/helpers';
import fs from 'node:fs';
import Database from 'better-sqlite3-multiple-ciphers';
import { WindowActivityEntity } from '../entities/WindowActivityEntity';
import { WindowActivityTrackerService } from './trackers/WindowActivityTrackerService';
import { Settings } from '../entities/Settings';

const LOG = getLogger('DataExportService');

export class DataExportService {
  private readonly windowActivityTrackerService: WindowActivityTrackerService =
    new WindowActivityTrackerService();
  public async startDataExport(
    windowActivityExportType: DataExportType,
    userInputExportType: DataExportType
  ): Promise<void> {
    LOG.info('startDataExport called');
    const dbName = 'database.sqlite';
    let dbPath = dbName;
    if (!(is.dev && process.env['VITE_DEV_SERVER_URL'])) {
      const userDataPath = app.getPath('userData');
      dbPath = path.join(userDataPath, dbName);
    }

    const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 });

    const userDataPath = app.getPath('userData');
    const exportFolderPath = path.join(userDataPath, 'exports');
    if (!fs.existsSync(exportFolderPath)) {
      fs.mkdirSync(exportFolderPath);
    }
    const now = new Date();
    const nowStr = now.toISOString().replace(/:/g, '-').replace('T', '_').slice(0, 16);
    const exportDbPath = path.join(
      userDataPath,
      'exports',
      `PA_${settings.subjectId}_${nowStr}.sqlite`
    );
    fs.copyFileSync(dbPath, exportDbPath);
    LOG.info(`Database copied to ${exportDbPath}`);
    const db = new Database(exportDbPath);
    // see https://github.com/WiseLibs/better-sqlite3/blob/master/docs/performance.md
    db.pragma('journal_mode = WAL');

    // see https://github.com/m4heshd/better-sqlite3-multiple-ciphers/issues/5#issuecomment-1008330548
    db.pragma(`cipher='sqlcipher'`);
    db.pragma(`legacy=4`);

    db.pragma(`rekey='PersonalAnalytics_${settings.subjectId}'`);

    if (windowActivityExportType === DataExportType.Obfuscate) {
      const items: { windowTitle: string; url: string; id: string }[] =
        await WindowActivityEntity.getRepository()
          .createQueryBuilder('window_activity')
          .select('id, windowTitle, url')
          .getRawMany();
      for (const item of items) {
        const randomizeWindowTitle = this.windowActivityTrackerService.randomizeWindowTitle(
          item.windowTitle
        );
        const randomizeUrl = this.windowActivityTrackerService.randomizeUrl(item.url);
        const obfuscateWindowActivities = db.prepare(
          'UPDATE window_activity SET windowTitle = ?, url = ? WHERE id = ?'
        );
        obfuscateWindowActivities.run(randomizeWindowTitle, randomizeUrl, item.id);
      }
    } else if (windowActivityExportType === DataExportType.None) {
      // remove all window activities
      const removeWindowActivities = db.prepare('DROP TABLE IF EXISTS window_activity');
      removeWindowActivities.run();
    }

    if (userInputExportType === DataExportType.None) {
      // remove all user input
      const removeUserInput = db.prepare('DROP TABLE IF EXISTS user_input');
      removeUserInput.run();
    }

    db.close();
  }
}
