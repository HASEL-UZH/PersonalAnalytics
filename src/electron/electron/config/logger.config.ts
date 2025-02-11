import { LevelOption } from 'electron-log';
import { is } from '../main/services/utils/helpers';

export const LOG_FILE_NAME = {
  BACKGROUND: 'background',
  RENDERER: 'renderer'
};

// Possible log levels: error, warn, info, verbose, debug, silly, and false to disable logging
// see: https://github.com/megahertz/electron-log#log-levels
export const LOG_LEVEL = {
  CONSOLE: 'silly' as LevelOption,
  FILE: (is.dev ? 'silly' : 'info') as LevelOption
};
