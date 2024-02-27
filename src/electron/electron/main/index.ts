import 'reflect-metadata';
import { app, dialog, powerMonitor, systemPreferences } from 'electron';
import { release } from 'node:os';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import log from 'electron-log/main';
import { getLogger } from '../shared/Logger';
import { DatabaseService } from './services/DatabaseService';
import { SettingsService } from './services/SettingsService';
import { TrackerType } from '../enums/TrackerType.enum';
import { WindowActivityTrackerService } from './services/trackers/WindowActivityTrackerService';
import { UserInputTrackerService } from './services/trackers/UserInputTrackerService';
import { TrackerService } from './services/trackers/TrackerService';
import AppUpdaterService from './services/AppUpdaterService';
import { WindowService } from './services/WindowService';
import { IpcHandler } from '../ipc/IpcHandler';
import { ExperienceSamplingService } from './services/ExperienceSamplingService';
import studyConfig from '../../shared/study.config';
import { is } from './services/utils/helpers';
import { Settings } from './entities/Settings';

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
const experienceSamplingService: ExperienceSamplingService = new ExperienceSamplingService();
const trackers: TrackerService = new TrackerService(studyConfig.trackers, windowService);
const ipcHandler: IpcHandler = new IpcHandler(windowService, trackers, experienceSamplingService);

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

if (is.macOS) {
  app.dock.hide();
}

// Optional, initialize the logger for any renderer process
log.initialize();
const LOG = getLogger('Main');

app.whenReady().then(async () => {
  app.setAppUserModelId('ch.ifi.hasel.personal-analytics');

  app.setLoginItemSettings({
    openAtLogin: true
  });

  try {
    await databaseService.init();
    await settingsService.init();
    await windowService.init();
    ipcHandler.init();
    await appUpdaterService.checkForUpdates({ silent: true });
    appUpdaterService.startCheckForUpdatesInterval();

    if (studyConfig.trackers.windowActivityTracker.enabled) {
      await trackers.registerTrackerCallback(
        TrackerType.WindowsActivityTracker,
        WindowActivityTrackerService.handleWindowChange
      );
    }
    if (studyConfig.trackers.userInputTracker.enabled) {
      await trackers.registerTrackerCallback(
        TrackerType.UserInputTracker,
        UserInputTrackerService.handleUserInputEvent
      );
    }
    if (studyConfig.trackers.experienceSamplingTracker.enabled) {
      await trackers.registerTrackerCallback(TrackerType.ExperienceSamplingTracker);
    }

    const settings: Settings = await Settings.findOneBy({ onlyOneEntityShouldExist: 1 });

    if (
      settings.onboardingShown === false ||
      !macOSHasAccessibilityAndScreenRecordingPermission()
    ) {
      LOG.debug(
        `Onboarding shown: ${settings.onboardingShown}, hasAccessibilityAndScreenRecordingPermission: ${macOSHasAccessibilityAndScreenRecordingPermission()}, creating onboarding window...`
      );
      await windowService.createOnboardingWindow(!is.macOS ? 'study-trackers-started' : undefined);
      settings.onboardingShown = true;
      await settings.save();
    }

    if (
      is.macOS &&
      settings.onboardingShown === true &&
      settings.studyAndTrackersStartedShown === false
    ) {
      await windowService.createOnboardingWindow('study-trackers-started');
      settings.studyAndTrackersStartedShown = true;
      await settings.save();
    }

    if (!is.macOS || macOSHasAccessibilityAndScreenRecordingPermission()) {
      LOG.debug(
        `Onboarding shown: ${settings.onboardingShown}, hasAccessibilityAndScreenRecordingPermission: ${macOSHasAccessibilityAndScreenRecordingPermission()}, starting all trackers...`
      );
      await trackers.startAllTrackers();
      LOG.info(`Trackers started: ${trackers.getRunningTrackerNames().join(', ')}`);

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
    }
  } catch (error) {
    LOG.error('Error during app initialization', error);
    dialog.showErrorBox(
      'Error during app initialization',
      `PersonalAnalytics couldn't be started. Please try again or contact us at ${studyConfig.contactEmail} for help. ${error}`
    );
  }
});

let isAppQuitting = false;
app.on('before-quit', async (event): Promise<void> => {
  LOG.info('app.on(before-quit) called');
  if (!isAppQuitting) {
    event.preventDefault();
    LOG.info(`Stopping all (${trackers.getRunningTrackerNames().join(', ')}) trackers...`);
    await trackers.stopAllTrackers();
    LOG.info(`All trackers stopped. Running: ${trackers.getRunningTrackerNames().length}`);
    isAppQuitting = true;
    app.exit();
  }
});

// Don't quit when all windows are closed
app.on('window-all-closed', () => {});

function macOSHasAccessibilityAndScreenRecordingPermission(): boolean {
  if (!is.macOS) {
    return true;
  }

  return (
    systemPreferences.isTrustedAccessibilityClient(false) &&
    systemPreferences.getMediaAccessStatus('screen') === 'granted'
  );
}
