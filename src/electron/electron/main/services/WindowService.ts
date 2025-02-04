import { app, BrowserWindow, Menu, nativeImage, screen, shell, Tray } from 'electron';
import getMainLogger from '../../config/Logger';
import AppUpdaterService from './AppUpdaterService';
import { is } from './utils/helpers';
import path from 'path';
import MenuItemConstructorOptions = Electron.MenuItemConstructorOptions;
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import studyConfig from '../../../shared/study.config';

import { Settings } from '../entities/Settings';
import { UsageDataService } from './UsageDataService';
import { UsageDataEventType } from '../../enums/UsageDataEventType.enum';

const LOG = getMainLogger('WindowService');

export class WindowService {
  private readonly appUpdaterService: AppUpdaterService;
  private tray: Tray;
  private experienceSamplingWindow: BrowserWindow;
  private aboutWindow: BrowserWindow;
  private onboardingWindow: BrowserWindow;
  private dataExportWindow: BrowserWindow;

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

  public async createExperienceSamplingWindow(isManuallyTriggered: boolean = false) {
    if (this.experienceSamplingWindow) {
      this.experienceSamplingWindow.close();
      this.experienceSamplingWindow = null;
    }

    const usageDataEvent = isManuallyTriggered
      ? UsageDataEventType.ExperienceSamplingManuallyOpened
      : UsageDataEventType.ExperienceSamplingAutomaticallyOpened;
    UsageDataService.createNewUsageDataEvent(usageDataEvent);

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

  public closeExperienceSamplingWindow(skippedExperienceSampling: boolean) {
    const usageDataEvent = skippedExperienceSampling
      ? UsageDataEventType.ExperienceSamplingSkipped
      : UsageDataEventType.ExperienceSamplingAnswered;
    UsageDataService.createNewUsageDataEvent(usageDataEvent);

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
      height: 850,
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

  public closeDataExportWindow() {
    if (this.dataExportWindow) {
      this.dataExportWindow?.close();
      this.dataExportWindow = null;
    }
  }

  public async createDataExportWindow() {
    this.closeDataExportWindow();

    const __filename = fileURLToPath(import.meta.url);
    const __dirname = dirname(__filename);
    const preload = join(__dirname, '../preload/index.mjs');
    this.dataExportWindow = new BrowserWindow({
      width: 1200,
      height: 850,
      show: false,
      minimizable: false,
      maximizable: false,
      minWidth: 1200,
      minHeight: 850,
      fullscreenable: false,
      title: 'PersonalAnalytics: Data Export',
      webPreferences: {
        preload
      }
    });

    if (process.env.VITE_DEV_SERVER_URL) {
      await this.dataExportWindow.loadURL(process.env.VITE_DEV_SERVER_URL + `#data-export`);
    } else {
      await this.dataExportWindow.loadFile(path.join(process.env.DIST, 'index.html'), {
        hash: `data-export?isMacOS`
      });
    }

    this.dataExportWindow.webContents.setWindowOpenHandler((details) => {
      shell.openExternal(details.url);
      return { action: 'deny' };
    });

    if (is.macOS && !is.dev) {
      const template = [
        {
          label: 'Edit',
          submenu: [
            { label: 'Copy', accelerator: 'CmdOrCtrl+C', selector: 'copy:' },
            { label: 'Paste', accelerator: 'CmdOrCtrl+V', selector: 'paste:' }
          ]
        }
      ];
      Menu.setApplicationMenu(Menu.buildFromTemplate(template));
    }

    if (is.macOS) {
      await app.dock.show();
    }

    this.dataExportWindow.show();

    this.dataExportWindow.on('close', () => {
      this.dataExportWindow = null;
      if (is.macOS) {
        app.dock.hide();
      }
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
    this.tray.on("click", () => { this.tray.popUpContextMenu(); });
    this.tray.setToolTip(`Personal Analytics is running ...\nYou are participating in: ${studyConfig.name}`);
  }

  private createTray(): void {
    LOG.debug('Creating tray');
    if (this.tray) {
      return;
    }
    const iconToUse = is.macOS ? 'IconTemplate.png' : 'IconColored@2x.png';
    const appIcon = path.join(process.env.VITE_PUBLIC, iconToUse);
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
        click: () => this.createExperienceSamplingWindow(true)
      },
      {
        label: 'Open Onboarding', // todo: only show for debug
        click: async () => {
          const shouldShowStudyTrackersStarted = !!(await Settings.findOneBy({
            studyAndTrackersStartedShown: false,
            onboardingShown: true
          }));
          await this.createOnboardingWindow(
            shouldShowStudyTrackersStarted ? 'study-trackers-started' : undefined
          );
        }
      },
      { type: 'separator' }
    ];
    const otherMenu: MenuItemConstructorOptions[] = [
      {
        label: 'About',
        click: () => this.createAboutWindow()
      },
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
        label: 'Open Logs', // todo: move to settings
        click: (): void => {
          LOG.info(`Opening logs at ${app.getPath('logs')}`);
          shell.openPath(`${app.getPath('logs')}`);
        }
      },
      {
        label: 'Open Collected Data', // todo: move to settings
        click: (): void => {
          LOG.info(`Opening collected data at ${app.getPath('userData')}`);
          shell.showItemInFolder(path.join(app.getPath('userData'), 'database.sqlite'));
        }
      },
      ...(studyConfig.dataExportEnabled
        ? [
            {
              label: 'Export Study Data',
              click: (): void => {
                LOG.info(`Opening data export`);
                this.createDataExportWindow();
              }
            }
          ]
        : []),
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
