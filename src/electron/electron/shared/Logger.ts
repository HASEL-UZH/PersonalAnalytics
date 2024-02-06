import logger from 'electron-log/main';
import { LogFunctions } from 'electron-log';
import { LOG_FILE_NAME, LOG_LEVEL } from '../config/logger.config';

const getLogger = (loggerName: string, background = false): LogFunctions => {
  const currentDate: string = new Date().toISOString().split('T')[0];
  const processName: string = background ? LOG_FILE_NAME.BACKGROUND : LOG_FILE_NAME.RENDERER;
  logger.transports.file.fileName = `${currentDate}-${processName}.log`;
  logger.transports.file.level = LOG_LEVEL.FILE;
  logger.transports.console.level = LOG_LEVEL.CONSOLE;
  return logger.scope(loggerName);
};

const logFile: string = logger.transports.file.getFile().path;
const LOG_PATH: string = logFile.substring(0, logFile.lastIndexOf('/'));

export { getLogger, LOG_PATH };
