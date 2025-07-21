import { DataExportType } from '../../../shared/DataExportType.enum';
import { net } from 'electron';
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
import { DataExportFormat } from '../../../shared/DataExportFormat.enum';
import archiver from 'archiver';
import axios from 'axios';
import FormData from 'form-data';

const LOG = getMainLogger('DataExportService');

export class DataExportService {
  private readonly windowActivityTrackerService: WindowActivityTrackerService =
    new WindowActivityTrackerService();
  public async startDataExport(
    windowActivityExportType: DataExportType,
    userInputExportType: DataExportType,
    obfuscationTerms: string[],
    encryptData: boolean,
    exportFormat: DataExportFormat,
  ): Promise<{ fullPath: string; fileName: string }> {
    LOG.info(`startDataExport called with ${exportFormat}`);
    await UsageDataService.createNewUsageDataEvent(
      UsageDataEventType.StartExport,
      JSON.stringify({
        exportFormat,
        windowActivityExportType,
        userInputExportType,
        obfuscationTermLength: obfuscationTerms?.length
      })
    );

    try {
      let exportPath: string;

      if (exportFormat === DataExportFormat.ExportAsSqlite) {
        exportPath = await this.exportToSqlite(
          windowActivityExportType,
          userInputExportType,
          obfuscationTerms,
          encryptData
        );
      } else if (exportFormat === DataExportFormat.ExportAsZippedSqlite) {
        exportPath = await this.exportAsZippedSqlite(
          windowActivityExportType,
          userInputExportType,
          obfuscationTerms,
          encryptData
        );
      } else if (exportFormat === DataExportFormat.ExportAsZippedJson) {
        exportPath = await this.exportAsZippedJson(
          windowActivityExportType,
          userInputExportType,
          obfuscationTerms,
          encryptData
        );
      } else if (exportFormat === DataExportFormat.ExportToDDL) {
        exportPath = await this.exportToDDL(
          windowActivityExportType,
          userInputExportType,
          obfuscationTerms,
          encryptData
        );
      } else {
        throw new Error(`Unsupported export format: ${exportFormat}`);
      }

      await UsageDataService.createNewUsageDataEvent(UsageDataEventType.FinishExport, JSON.stringify({exportFormat}));

      return {
        fullPath: exportPath, 
        fileName: path.basename(exportPath) 
      };

    } catch (error) {
      LOG.error(`Error exporting the data as ${exportFormat}`, error);
      throw error;
    }
  }

  private async exportToSqlite(
    windowActivityExportType: DataExportType,
    userInputExportType: DataExportType,
    obfuscationTerms: string[],
    encryptData: boolean,
  ): Promise<string> {

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
    const exportDbPath = path.join(userDataPath, 'exports', `PA_${settings.subjectId}_${nowStr}.sqlite`);
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
      windowActivityExportType === DataExportType.ObfuscateWithTerms
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

    // run VACUUm to reduce the database size after data is dropped
    db.prepare('VACUUM').run();

    db.close();

    return exportDbPath;
  }

  private async exportAsZippedSqlite(
    windowActivityExportType: DataExportType,
    userInputExportType: DataExportType,
    obfuscationTerms: string[],
    encryptData: boolean,
  ): Promise<string> {
    const sqlitePath = await this.exportToSqlite(
      windowActivityExportType,
      userInputExportType,
      obfuscationTerms,
      encryptData
    );

    const zipPath = sqlitePath.replace(/\.sqlite$/, '.zip');
    const zipOutput = fs.createWriteStream(zipPath);
    const archive = archiver('zip', { zlib: { level: 6 } }); // level 0 is no compression, level 9 is max compression

    return new Promise<string>((resolve, reject) => {
      zipOutput.on('close', () => {
        LOG.info(`Exported and zipped to ${zipPath} (${archive.pointer()} total bytes)`);
  
        // Delete the original .sqlite file after zipping
        fs.unlink(sqlitePath, (err) => {
          if (err) {
            LOG.warn(`Failed to delete temporary sqlite file: ${sqlitePath}`, err);
          } else {
            LOG.info(`Deleted temporary sqlite file: ${sqlitePath}`);
          }
          resolve(zipPath);
        });
      });
  
      archive.on('error', (err) => {
        reject(err);
      });
  
      archive.pipe(zipOutput);
      archive.file(sqlitePath, { name: path.basename(sqlitePath) });
      archive.finalize();
    });
  }

  private async exportAsZippedJson(
    windowActivityExportType: DataExportType,
    userInputExportType: DataExportType,
    obfuscationTerms: string[],
    encryptData: boolean,
  ): Promise<string> {
    const sqlitePath = await this.exportToSqlite(
      windowActivityExportType,
      userInputExportType,
      obfuscationTerms,
      encryptData
    );

    const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 });
    const userDataPath = app.getPath('userData');
    const exportFolderPath = path.join(userDataPath, 'exports');
    const tempJsonFiles: string[] = [];
    
    // create database and read all tables
    const db = new Database(sqlitePath);
    const tables = db.prepare(
      "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';"
    ).all().map((row: any) => row.name);

    // create json file for each table
    for (const table of tables) {
      const rows = db.prepare(`SELECT * FROM ${table}`).all();
      const jsonPath = path.join(exportFolderPath, `PA_${settings.subjectId}_${table}.json`);
      fs.writeFileSync(jsonPath, JSON.stringify(rows, null, 2));
      tempJsonFiles.push(jsonPath);
    }
    db.close();

    // create zip file with all json files
    const zipPath = sqlitePath.replace(/\.sqlite$/, '.zip');
    const zipOutput = fs.createWriteStream(zipPath);
    const archive = archiver('zip', { zlib: { level: 0 } });

    return new Promise<string>((resolve, reject) => {
      zipOutput.on('close', () => {
        LOG.info(`Exported and zipped to ${zipPath} (${archive.pointer()} total bytes)`);

        // Cleanup: delete the temporary .json and .sqlite files
        for (const file of tempJsonFiles) {
          fs.unlink(file, (err) => {
            if (err) LOG.warn(`Failed to delete the temporary json file: ${file}`, err);
          });
        }
        fs.unlink(sqlitePath, (err) => {
          if (err) LOG.warn(`Failed to delete temporary sqlite file: ${sqlitePath}`, err);
        });

        resolve(zipPath);
      });

      archive.on('error', (err) => {
        reject(err);
      });

      archive.pipe(zipOutput);
      for (const file of tempJsonFiles) {
        archive.file(file, { name: path.basename(file) });
      }
      archive.finalize();
    });
  }

  private async exportToDDL(
    windowActivityExportType: DataExportType,
    userInputExportType: DataExportType,
    obfuscationTerms: string[],
    encryptData: boolean,
  ): Promise<string> {

    if (net.isOnline()) {
      const zipPath = await this.exportAsZippedJson(
        windowActivityExportType,
        userInputExportType,
        obfuscationTerms,
        encryptData
      );
  
      const projectId = process.env.DDL_PROJECT_ID || 'add for debugging'; // set in Github Secrets
      const projectToken = process.env.DDL_PROJECT_TOKEN || 'add for debugging'; // set in Github Secrets (expires after maximum of 90d)
      const url = `https://datadonation.uzh.ch/api/zip/${projectId}`;
  
      const form = new FormData();
      form.append('file', fs.createReadStream(zipPath));
  
      try {
        const response = await axios.post(url, form, {
          headers: {
            ...form.getHeaders(),
            Authorization: `Token ${projectToken}`,
          },
          maxContentLength: Infinity,
          maxBodyLength: Infinity,
        });
        
        if (response.status !== 201) {
          throw new Error(`Failed to upload to DDL: ${response.statusText}`);
        }
        LOG.info(`Uploaded to DDL: status ${response.status}, response: ${response.data}`);
  
        // option to delete the zip file after upload (but we're keeping it for now)
        // fs.unlink(zipPath, (err) => {
        //   if (err) LOG.warn(`Failed to delete temporary zipped json file: ${zipPath}`, err);
        // });
  
        return zipPath;
      } catch (error) {
        LOG.error(`Failed to upload to DDL`, error);
        throw error;
      }
    } else {
      LOG.info("No internet connection, skipping upload to DDL.");
      throw new Error("No internet connection, skipping upload to DDL."); 
    }
  }
}
