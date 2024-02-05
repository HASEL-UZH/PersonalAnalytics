import { ipcRenderer, contextBridge } from 'electron';

contextBridge.exposeInMainWorld('ipcRenderer', withPrototype(ipcRenderer));

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
