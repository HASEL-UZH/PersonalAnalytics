import { app, BrowserWindow, Menu, nativeImage, screen, shell, Tray } from 'electron';
import { getLogger } from '../../shared/Logger';
import AppUpdaterService from './AppUpdaterService';
import { is } from './utils/helpers';
import path from 'path';
import MenuItemConstructorOptions = Electron.MenuItemConstructorOptions;
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import studyConfig from '../../../shared/study.config';

const LOG = getLogger('WindowService');

export class WindowService {
  private readonly appUpdaterService: AppUpdaterService;
  private tray: Tray;
  private experienceSamplingWindow: BrowserWindow;
  private aboutWindow: BrowserWindow;
  private onboardingWindow: BrowserWindow;

  constructor(appUpdaterService: AppUpdaterService) {
    this.appUpdaterService = appUpdaterService;
    LOG.debug('WindowService constructor called');

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
    this.closeExperienceSamplingWindow();

    const __filename = fileURLToPath(import.meta.url);
    const __dirname = dirname(__filename);
    const preload = join(__dirname, '../preload/index.mjs');

    const { width } = screen.getPrimaryDisplay().workAreaSize;
    const windowPadding = 20;
    const windowWidth = 500;
    const windowHeight = 170;

    this.experienceSamplingWindow = new BrowserWindow({
      width: windowWidth,
      height: windowHeight,
      x: width - windowWidth - windowPadding,
      y: 20 + windowPadding * 2,
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
      title: 'PersonalAnalytics: Self-Report',
      webPreferences: {
        preload
      }
    });

    if (process.env.VITE_DEV_SERVER_URL) {
      await this.experienceSamplingWindow.loadURL(
        process.env.VITE_DEV_SERVER_URL + '#experience-sampling'
      );
    } else {
      await this.experienceSamplingWindow.loadFile(path.join(process.env.DIST, 'index.html'), {
        hash: 'experience-sampling'
      });
    }

    this.experienceSamplingWindow.setVisibleOnAllWorkspaces(true);
    let opacity = 0;
    const interval = setInterval(() => {
      if (opacity >= 1) clearInterval(interval);
      this.experienceSamplingWindow?.setOpacity(opacity);
      opacity += 0.1;
    }, 10);
    this.experienceSamplingWindow.showInactive();

    this.experienceSamplingWindow.on('close', () => {
      this.experienceSamplingWindow = null;
    });
  }

  public closeExperienceSamplingWindow() {
    if (this.experienceSamplingWindow) {
      this.experienceSamplingWindow.close();
      this.experienceSamplingWindow = null;
    }
  }

  private closeAboutWindow() {
    if (this.aboutWindow) {
      this.aboutWindow?.close();
      this.aboutWindow = null;
    }
  }

  public async createAboutWindow() {
    this.closeAboutWindow();

    const __filename = fileURLToPath(import.meta.url);
    const __dirname = dirname(__filename);
    const preload = join(__dirname, '../preload/index.mjs');
    this.aboutWindow = new BrowserWindow({
      width: 800,
      height: 750,
      show: false,
      minimizable: false,
      maximizable: false,
      fullscreenable: false,
      resizable: false,
      title: 'PersonalAnalytics: About',
      webPreferences: {
        preload
      }
    });

    if (process.env.VITE_DEV_SERVER_URL) {
      await this.aboutWindow.loadURL(process.env.VITE_DEV_SERVER_URL + '#about');
    } else {
      await this.aboutWindow.loadFile(path.join(process.env.DIST, 'index.html'), {
        hash: 'about'
      });
    }

    this.aboutWindow.webContents.setWindowOpenHandler((details) => {
      shell.openExternal(details.url);
      return { action: 'deny' };
    });

    this.aboutWindow.show();

    this.aboutWindow.on('close', () => {
      this.aboutWindow = null;
    });
  }

  public closeOnboardingWindow() {
    if (this.onboardingWindow) {
      this.onboardingWindow?.close();
      this.onboardingWindow = null;
    }
  }

  public async createOnboardingWindow(goToStep?: string) {
    this.closeOnboardingWindow();

    const __filename = fileURLToPath(import.meta.url);
    const __dirname = dirname(__filename);
    const preload = join(__dirname, '../preload/index.mjs');
    this.onboardingWindow = new BrowserWindow({
      width: 800,
      height: 750,
      show: false,
      minimizable: false,
      maximizable: false,
      fullscreenable: false,
      resizable: false,
      title: 'PersonalAnalytics: About',
      webPreferences: {
        preload
      }
    });

    if (process.env.VITE_DEV_SERVER_URL) {
      await this.onboardingWindow.loadURL(
        process.env.VITE_DEV_SERVER_URL +
          `#onboarding?isMacOS=${is.macOS}&goToStep=${goToStep ?? 'welcome'}`
      );
    } else {
      await this.onboardingWindow.loadFile(path.join(process.env.DIST, 'index.html'), {
        hash: `onboarding?isMacOS=${is.macOS}&goToStep=${goToStep ?? 'welcome'}`
      });
    }

    this.onboardingWindow.webContents.setWindowOpenHandler((details) => {
      shell.openExternal(details.url);
      return { action: 'deny' };
    });

    this.onboardingWindow.show();

    this.onboardingWindow.on('close', () => {
      this.onboardingWindow = null;
    });
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
    this.tray.setToolTip(studyConfig.name);
  }

  private createTray(): void {
    LOG.debug('Creating tray');
    if (this.tray) {
      return;
    }
    const appIcon = path.join(process.env.VITE_PUBLIC, 'IconTemplate.png');
    const trayImage = nativeImage.createFromPath(appIcon);
    trayImage.setTemplateImage(true);
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
      {
        label: 'Open Onboarding',
        click: () => this.createOnboardingWindow()
      },
      {
        label: 'About',
        click: () => this.createAboutWindow()
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
          shell.openPath(`${app.getPath('logs')}`);
        }
      },
      {
        label: 'Open Collected Data',
        click: (): void => {
          LOG.info(`Opening collected data at ${app.getPath('userData')}`);
          shell.showItemInFolder(path.join(app.getPath('userData'), 'database.sqlite'));
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
