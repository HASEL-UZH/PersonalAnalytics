import { app, BrowserWindow, Menu, nativeImage, screen, shell, Tray } from 'electron';
import { getLogger } from '../../shared/Logger';
import AppUpdaterService from './AppUpdaterService';
import { is } from './utils/helpers';
import path from 'path';
import studyConfig from '../../config/study.config';
import MenuItemConstructorOptions = Electron.MenuItemConstructorOptions;
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const LOG = getLogger('WindowService');

export class WindowService {
  private readonly appUpdaterService: AppUpdaterService;
  private tray: Tray;
  private readonly isDevelopment: boolean = is.dev;

  constructor(appUpdaterService: AppUpdaterService) {
    this.appUpdaterService = appUpdaterService;

    this.appUpdaterService.on(
      'update-tray',
      ({ label, enabled }: { label: string; enabled: boolean }) => {
        this.updateTray(label, enabled);
      }
    );
    if (!is.dev) {
      Menu.setApplicationMenu(null);
    }
  }

  public async init(): Promise<void> {
    this.createTray();
  }

  public async createExperienceSamplingWindow() {
    const __filename = fileURLToPath(import.meta.url);
    const __dirname = dirname(__filename);
    const preload = join(__dirname, '../preload/index.mjs');

    const { width } = screen.getPrimaryDisplay().workAreaSize;
    const windowPadding = 20;
    const windowWidth = 500;
    const windowHeight = 130;

    const win = new BrowserWindow({
      width: windowWidth,
      height: windowHeight,
      x: width - windowWidth - windowPadding,
      y: 20 + windowPadding,
      show: false,
      opacity: 0,
      frame: false,
      alwaysOnTop: true,
      visualEffectState: 'inactive',
      minimizable: false,
      maximizable: false,
      fullscreenable: false,
      resizable: false,
      acceptFirstMouse: true,
      title: 'Daily Planning',
      webPreferences: {
        preload
      }
    });

    if (process.env.VITE_DEV_SERVER_URL) {
      await win.loadURL(process.env.VITE_DEV_SERVER_URL + '#experience-sampling');
    } else {
      await win.loadFile(path.join(process.env.DIST, 'index.html'), {
        hash: 'experience-sampling'
      });
    }

    win.setVisibleOnAllWorkspaces(true);
    let opacity = 0;
    const interval = setInterval(() => {
      if (opacity >= 1) clearInterval(interval);
      win?.setOpacity(opacity);
      opacity += 0.1;
    }, 10);
    win.show();
  }

  public updateTray(
    updaterLabel: string = 'Check for updates',
    updaterMenuEnabled: boolean = false
  ): void {
    LOG.debug('Updating tray');
    const menuTemplate: MenuItemConstructorOptions[] = this.getTrayMenuTemplate();
    menuTemplate[1].label = updaterLabel;
    menuTemplate[1].enabled = updaterMenuEnabled;

    this.tray.setContextMenu(Menu.buildFromTemplate(menuTemplate));
  }

  private createTray(): void {
    LOG.debug('Creating tray');
    if (this.tray) {
      return;
    }
    const appIcon = path.join(process.env.VITE_PUBLIC, 'icon-tray.png');
    const trayImage = nativeImage.createFromPath(appIcon);
    this.tray = new Tray(trayImage);
    this.updateTray();
  }

  private getTrayMenuTemplate(): MenuItemConstructorOptions[] {
    const versionAndUpdate: MenuItemConstructorOptions[] = [
      { label: `Version ${app.getVersion()}`, enabled: false },
      {
        label: 'Check for updates',
        enabled: false,
        click: () => this.appUpdaterService.checkForUpdates({ silent: false })
      },
      { type: 'separator' }
    ];
    const windowMenu: MenuItemConstructorOptions[] = [
      {
        label: 'Open Experience Sampling',
        click: () => this.createExperienceSamplingWindow()
      },
      { type: 'separator' }
    ];
    const otherMenu: MenuItemConstructorOptions[] = [
      {
        label: 'Get Help',
        click: (): void => {
          const mailToAddress = studyConfig.contactEmail;
          shell.openExternal(`mailto:${mailToAddress}`);
        }
      },
      {
        label: 'Report a Problem',
        click: (): void => {
          const mailToAddress = studyConfig.contactEmail;
          shell.openExternal(`mailto:${mailToAddress}`);
        }
      },
      { type: 'separator' },
      {
        label: 'Open Logs',
        click: (): void => {
          LOG.info(`Opening logs at ${app.getPath('logs')}`);
          shell.showItemInFolder(`${app.getPath('logs')}`);
        }
      },
      { type: 'separator' },
      {
        label: 'Quit',
        click: () => {
          app.quit();
        }
      }
    ];
    return [...versionAndUpdate, ...windowMenu, ...otherMenu];
  }
}
