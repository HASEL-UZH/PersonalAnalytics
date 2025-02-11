import logger from 'electron-log/main';
import { LogFunctions } from 'electron-log';
import { LOG_FILE_NAME, LOG_LEVEL } from './logger.config';

export const getMainLogger = (loggerName: string): LogFunctions => {
  logger.transports.file.fileName = `${LOG_FILE_NAME.BACKGROUND}.log`;
  logger.transports.file.maxSize = 15 * 1024 * 1024;
  logger.transports.file.level = LOG_LEVEL.FILE;
  logger.transports.console.level = LOG_LEVEL.CONSOLE;
  return logger.scope(loggerName);
};

export default getMainLogger;
