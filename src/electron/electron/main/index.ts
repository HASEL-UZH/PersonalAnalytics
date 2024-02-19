import 'reflect-metadata';
import '../ipc/Api';
import { app, BrowserWindow, powerMonitor } from 'electron';
import { release } from 'node:os';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import log from 'electron-log/main';
import { getLogger } from '../shared/Logger';
import { DatabaseService } from './services/DatabaseService';
import { SettingsService } from './services/SettingsService';
import studyConfig from '../config/study.config';
import { TrackerType } from '../enums/TrackerType.enum';
import { WindowActivityTrackerService } from './services/trackers/WindowActivityTrackerService';
import { UserInputTrackerService } from './services/trackers/UserInputTrackerService';
import { TrackerService } from './services/trackers/TrackerService';
import AppUpdaterService from './services/AppUpdaterService';
import { WindowService } from './services/WindowService';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

process.env.DIST_ELECTRON = join(__dirname, '..');
process.env.DIST = join(process.env.DIST_ELECTRON, '../dist');
process.env.VITE_PUBLIC = process.env.VITE_DEV_SERVER_URL
  ? join(process.env.DIST_ELECTRON, '../public')
  : process.env.DIST;

const databaseService: DatabaseService = new DatabaseService();
const settingsService: SettingsService = new SettingsService();
const appUpdaterService: AppUpdaterService = new AppUpdaterService();
const windowService: WindowService = new WindowService(appUpdaterService);

// Disable GPU Acceleration for Windows 7
if (release().startsWith('6.1')) {
  app.disableHardwareAcceleration();
}

// Set application name for Windows 10+ notifications
if (process.platform === 'win32') {
  app.setAppUserModelId(app.getName());
}

if (!app.requestSingleInstanceLock()) {
  app.quit();
  process.exit(0);
}

// Optional, initialize the logger for any renderer process
log.initialize();
const LOG = getLogger('Main', true);
LOG.info('Log from the main process');

let win: BrowserWindow | null = null;
const preload = join(__dirname, '../preload/index.mjs');
const url = process.env.VITE_DEV_SERVER_URL;
const indexHtml = join(process.env.DIST, 'index.html');

async function createWindow() {
  win = new BrowserWindow({
    title: 'Main window',
    icon: join(process.env.VITE_PUBLIC, 'favicon.ico'),
    webPreferences: {
      preload
    }
  });

  if (process.env.VITE_DEV_SERVER_URL) {
    // electron-vite-vue#298
    win.loadURL(url);
    win.webContents.openDevTools();
  } else {
    win.loadFile(indexHtml);
  }
}

app.whenReady().then(async () => {
  app.setAppUserModelId('ch.ifi.hasel.personal-analytics');

  app.setLoginItemSettings({
    openAtLogin: true
  });

  await databaseService.init();
  await settingsService.init();
  await windowService.init();
  await appUpdaterService.checkForUpdates({ silent: true });
  appUpdaterService.startCheckForUpdatesInterval();

  const trackers: TrackerService = new TrackerService(studyConfig.trackers);
  await trackers.registerTrackerCallback(
    TrackerType.WindowsActivityTracker,
    WindowActivityTrackerService.handleWindowChange
  );

  await trackers.registerTrackerCallback(
    TrackerType.UserInputTracker,
    UserInputTrackerService.handleUserInputEvent
  );

  await trackers.startAllTrackers();
  LOG.info(`Trackers started: ${trackers.getRunningTrackerNames()}`);

  powerMonitor.on('suspend', async (): Promise<void> => {
    LOG.debug('The system is going to sleep');
    await trackers.stopAllTrackers();
  });
  powerMonitor.on('resume', async (): Promise<void> => {
    LOG.debug('The system is resuming');
    await trackers.startAllTrackers();
  });
  powerMonitor.on('shutdown', async (): Promise<void> => {
    LOG.debug('The system is going to shutdown');
    await trackers.stopAllTrackers();
  });
  powerMonitor.on('lock-screen', async (): Promise<void> => {
    LOG.debug('The system is going to lock-screen');
    await trackers.stopAllTrackers();
  });
  powerMonitor.on('unlock-screen', async (): Promise<void> => {
    LOG.debug('The system is going to unlock-screen');
    await trackers.startAllTrackers();
  });
});

app.on('window-all-closed', () => {
  win = null;
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('activate', () => {
  const allWindows = BrowserWindow.getAllWindows();
  if (allWindows.length) {
    allWindows[0].focus();
  } else {
    createWindow();
  }
});
