import { ipcRenderer, contextBridge } from 'electron';

// Electron 29 no longer allows the native ipcRenderer object to cross the context bridge. Expose
// only the renderer operation used by the application through a plain wrapper instead.
contextBridge.exposeInMainWorld('ipcRenderer', {
  invoke: (channel: string, ...args: unknown[]) => ipcRenderer.invoke(channel, ...args)
});
