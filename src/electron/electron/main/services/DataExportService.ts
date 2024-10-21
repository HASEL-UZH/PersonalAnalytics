import { DataExportType } from '../../../shared/DataExportType.enum';
import getMainLogger from '../../config/Logger';
import path from 'path';
import { app } from 'electron';
import { is } from './utils/helpers';
import fs from 'node:fs';
import Database from 'better-sqlite3-multiple-ciphers';
import { WindowActivityEntity } from '../entities/WindowActivityEntity';
import { WindowActivityTrackerService } from './trackers/WindowActivityTrackerService';
import { Settings } from '../entities/Settings';
import { UsageDataService } from './UsageDataService';
import { UsageDataEventType } from '../../enums/UsageDataEventType.enum';

const LOG = getMainLogger('DataExportService');

export class DataExportService {
  private readonly windowActivityTrackerService: WindowActivityTrackerService =
    new WindowActivityTrackerService();
  public async startDataExport(
    windowActivityExportType: DataExportType,
    userInputExportType: DataExportType,
    obfuscationTerms: string[],
    encryptData: boolean
  ): Promise<string> {
    LOG.info('startDataExport called');
    await UsageDataService.createNewUsageDataEvent(
      UsageDataEventType.StartExport,
      JSON.stringify({
        windowActivityExportType,
        userInputExportType,
        obfuscationTermLength: obfuscationTerms?.length
      })
    );
    try {
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
      // Also update the DataExportView if you change the file name here
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

      if (encryptData) {
        // see https://github.com/m4heshd/better-sqlite3-multiple-ciphers/issues/5#issuecomment-1008330548
        db.pragma(`cipher='sqlcipher'`);
        db.pragma(`legacy=4`);

        db.pragma(`rekey='PersonalAnalytics_${settings.subjectId}'`);
      }

      if (
        windowActivityExportType === DataExportType.Obfuscate ||
        DataExportType.ObfuscateWithTerms
      ) {
        const items: {
          windowTitle: string;
          url: string;
          id: string;
        }[] = await WindowActivityEntity.getRepository()
          .createQueryBuilder('window_activity')
          .select('id, windowTitle, url')
          .getRawMany();
        for (const item of items) {
          if (windowActivityExportType === DataExportType.Obfuscate) {
            const randomizeWindowTitle = this.windowActivityTrackerService.randomizeString(
              item.windowTitle
            );
            const randomizeUrl = this.windowActivityTrackerService.randomizeUrl(item.url);
            const obfuscateWindowActivities = db.prepare(
              'UPDATE window_activity SET windowTitle = ?, url = ? WHERE id = ?'
            );
            obfuscateWindowActivities.run(randomizeWindowTitle, randomizeUrl, item.id);
          } else if (
            windowActivityExportType === DataExportType.ObfuscateWithTerms &&
            obfuscationTerms.length > 0
          ) {
            const lowerCaseObfuscationTerms: string[] = obfuscationTerms.map((term: string) =>
              term.toLowerCase()
            );
            lowerCaseObfuscationTerms.forEach((term: string) => {
              if (
                item.windowTitle?.toLowerCase().includes(term) ||
                item.url?.toLowerCase().includes(term)
              ) {
                const obfuscateWindowActivities = db.prepare(
                  'UPDATE window_activity SET windowTitle = ?, url = ? WHERE id = ?'
                );
                const windowTitle = item.windowTitle ? '[anonymized]' : undefined;
                const url = item.url ? '[anonymized]' : undefined;

                obfuscateWindowActivities.run(windowTitle, url, item.id);
              }
            });
          }
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

      await UsageDataService.createNewUsageDataEvent(UsageDataEventType.FinishExport);

      return exportDbPath;
    } catch (error) {
      LOG.error('Error exporting the data', error);
      throw error;
    }
  }
}
