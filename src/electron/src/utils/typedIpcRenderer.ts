import Events from './Events';
import Commands from './Commands';
import { TypedIpcRenderer } from './TypedIpcMain';

const typedIpcRenderer = window.ipcRenderer as TypedIpcRenderer<Events, Commands>;

export default typedIpcRenderer;
