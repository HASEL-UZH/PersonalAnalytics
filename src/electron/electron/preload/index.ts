import { ipcRenderer, contextBridge } from 'electron';

// ***AIRBAR - START
const api: Api = {
  onLoadTaskbarTasks: (callback) => {
    ipcRenderer.on('loadTaskbarTasks', callback);
  },
  onRemindToTrackTime: (callback) =>
    ipcRenderer.on('remindToTrackTime', (_event, reason) => callback(reason)),
  onTaskWidgetWindowFocused: (callback) => ipcRenderer.on('taskWidgetWindowFocused', callback),
  isMacOS: (): boolean => process.platform === 'darwin'
};

interface Api {
  onLoadTaskbarTasks: (cb) => void;
  onRemindToTrackTime: (cb) => void;
  onTaskWidgetWindowFocused: (cb) => void;
  isMacOS: () => boolean;
}

declare global {
  interface Window {
    ipcRenderer: Record<string, any>;
    api: Api;
  }
}

contextBridge.exposeInMainWorld('api', withPrototype(api));
window.api = api;
// ***AIRBAR - END

contextBridge.exposeInMainWorld('ipcRenderer', withPrototype(ipcRenderer));
window.ipcRenderer = ipcRenderer;

// `exposeInMainWorld` can't detect attributes and methods of `prototype`, manually patching it.
// eslint-disable-next-line @typescript-eslint/no-explicit-any
function withPrototype(obj: Record<string, any>) {
  const protos = Object.getPrototypeOf(obj);

  for (const [key, value] of Object.entries(protos)) {
    if (Object.prototype.hasOwnProperty.call(obj, key)) continue;

    if (typeof value === 'function') {
      // Some native APIs, like `NodeJS.EventEmitter['on']`, don't work in the Renderer process. Wrapping them into a function.
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      obj[key] = function (...args: any) {
        return value.call(obj, ...args);
      };
    } else {
      obj[key] = value;
    }
  }
  return obj;
}
