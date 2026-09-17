import { ipcRenderer, contextBridge } from 'electron';

// Expose only the IPC operation used by the renderer. Copying ipcRenderer's prototype is brittle
// across Electron releases and stopped exposing invoke() in Electron 43.
contextBridge.exposeInMainWorld('ipcRenderer', {
  invoke: (channel: string, ...args: unknown[]) => ipcRenderer.invoke(channel, ...args)
});
