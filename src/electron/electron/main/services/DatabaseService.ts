import { DataSource, DataSourceOptions } from 'typeorm';
import { app } from 'electron';
import path from 'path';
import { is } from './utils/helpers';
import getMainLogger from '../../config/Logger';
import { WindowActivityEntity } from '../entities/WindowActivityEntity';
import { ExperienceSamplingResponseEntity } from '../entities/ExperienceSamplingResponseEntity';
import { UserInputEntity } from '../entities/UserInputEntity';
import { Settings } from '../entities/Settings';
import { UsageDataEntity } from '../entities/UsageDataEntity';
import { WorkDayEntity } from '../entities/WorkDayEntity'

const LOG = getMainLogger('DatabaseService');

export class DatabaseService {
  public dataSource: DataSource;
  private readonly dbPath: string;

  constructor() {
    const dbName = 'database.sqlite';
    this.dbPath = dbName;
    if (!(is.dev && process.env['VITE_DEV_SERVER_URL'])) {
      const userDataPath = app.getPath('userData');
      this.dbPath = path.join(userDataPath, dbName);
    }
    LOG.info('Using database path:', this.dbPath);
  }
  
  public async init(): Promise<void> {
    let entities: any = [
      ExperienceSamplingResponseEntity,
      Settings,
      UsageDataEntity,
      UserInputEntity,
      WindowActivityEntity,
      WorkDayEntity
    ]
    
    let options: DataSourceOptions = {
      type: 'better-sqlite3',
      database: this.dbPath,
      synchronize: true,
      logging: false,
      entities: entities,
    };
    
    this.dataSource = new DataSource(options);

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
