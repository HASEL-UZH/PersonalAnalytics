import { DataSource, DataSourceOptions } from 'typeorm';
import { app } from 'electron';
import path from 'path';
import { is } from './utils/helpers';
import { getLogger } from '../../shared/Logger';
import { WindowActivityEntity } from '../entities/WindowActivityEntity';
import { ExperienceSamplingResponseEntity } from '../entities/ExperienceSamplingResponseEntity';
import { UserInputEntity } from '../entities/UserInputEntity';
import { Settings } from '../entities/Settings';

const LOG = getLogger('DatabaseService');

export class DatabaseService {
  public dataSource: DataSource;
  private readonly options: DataSourceOptions;

  constructor() {
    const dbName = 'database.sqlite';
    let dbPath = dbName;
    if (!(is.dev && process.env['VITE_DEV_SERVER_URL'])) {
      const userDataPath = app.getPath('userData');
      dbPath = path.join(userDataPath, dbName);
    }
    LOG.info('Using database path:', dbPath);
    this.options = {
      type: 'better-sqlite3',
      database: dbPath,
      synchronize: true,
      logging: true,
      entities: [ExperienceSamplingResponseEntity, Settings, UserInputEntity, WindowActivityEntity]
    };

    this.dataSource = new DataSource(this.options);
  }

  public async init(): Promise<void> {
    try {
      await this.dataSource.initialize();
      LOG.info('Database connection established');
    } catch (error) {
      LOG.error('Database connection failed', error);
    }
  }

  public async clearDatabase(): Promise<void> {
    try {
      LOG.info('Dropping database');
      await this.dataSource.dropDatabase();
      LOG.info('Database dropped');
      LOG.info('Synchronizing database');
      await this.dataSource.synchronize();
      LOG.info('Database synchronized');
      LOG.info('Database successfully cleared');
    } catch (error) {
      LOG.error('Database clearing failed', error);
    }
  }
}
