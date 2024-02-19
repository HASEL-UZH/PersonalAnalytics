import { TypedIpcRenderer } from '../../electron/ipc/TypedIpcMain';
import Events from '../../electron/shared/Events';
import Commands from '../../electron/shared/Commands';

const typedIpcRenderer = window.ipcRenderer as TypedIpcRenderer<Events, Commands>;

export default typedIpcRenderer;
