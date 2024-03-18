import { LogFunctions } from 'electron-log';
import rendererLogger from 'electron-log/renderer';

const getRendererLogger = (loggerName: string): LogFunctions => {
  return rendererLogger.scope(`Renderer/${loggerName}`);
};
export default getRendererLogger;
