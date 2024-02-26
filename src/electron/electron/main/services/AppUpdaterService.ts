import { getLogger } from '../../shared/Logger';
import { dialog, net } from 'electron';
import { EventEmitter } from 'events';
import updater from 'electron-updater';

const { autoUpdater } = updater;

const LOG = getLogger('AutoUpdater');

export default class AppUpdaterService extends EventEmitter {
  private checkForUpdatesInterval: NodeJS.Timeout | undefined;
  private isSilentCheckForUpdates: boolean = false;
  private updateDownloaded: boolean = false;
  private failedUpdateAttempts: number = 0;

  constructor() {
    super();
    autoUpdater.logger = LOG;
    autoUpdater.autoDownload = false;

    LOG.debug('AppUpdaterService constructor called');

    autoUpdater.on('checking-for-update', () => {
      LOG.info('Checking for update...');
    });

    autoUpdater.on('update-available', async (info) => {
      LOG.info(`Update available. Version: ${info.version}`);

      if (this.isSilentCheckForUpdates) {
        autoUpdater.downloadUpdate();
        return;
      }
      const dialogResponse = await dialog.showMessageBox({
        type: 'info',
        title: 'Found PersonalAnalytics Update',
        message: 'New update available, do you want to download the update now?',
        defaultId: 0,
        cancelId: 1,
        buttons: ['Yes', 'No']
      });
      if (dialogResponse.response === 0) {
        autoUpdater.downloadUpdate();
      } else {
        this.changeUpdaterMenu({ label: 'Check for updates', enabled: true });
      }
    });

    autoUpdater.on('error', (error) => {
      LOG.error(`Error in autoUpdater: ${error}`);
      this.failedUpdateAttempts++;
      this.changeUpdaterMenu({ label: 'Check for updates', enabled: true });
      if (this.failedUpdateAttempts === 3) {
        this.failedUpdateAttempts = 0;
        dialog.showErrorBox(
          'Error during the update',
          `PersonalAnalytics couldn't be updated. Please try again or contact me at sebastian.richner@uzh.ch`
        );
      }
    });

    autoUpdater.on('update-not-available', () => {
      LOG.info('Update not available.');
      this.failedUpdateAttempts = 0;
      this.changeUpdaterMenu({ label: 'Check for updates', enabled: true });
      if (this.isSilentCheckForUpdates) return;
      dialog.showMessageBox({
        title: 'No Updates',
        message: 'Current PersonalAnalytics version is up-to-date.'
      });
    });

    autoUpdater.on('update-downloaded', async () => {
      LOG.info('Update downloaded');
      this.failedUpdateAttempts = 0;
      this.updateDownloaded = true;
      this.changeUpdaterMenu({ label: 'Updates available', enabled: true });
      const dialogResponse = await dialog.showMessageBox({
        title: 'Install PersonalAnalytics Updates',
        message: 'Updates are ready to be installed.',
        defaultId: 0,
        cancelId: 1,
        buttons: ['Install and restart', 'Close']
      });
      if (dialogResponse.response === 0) {
        setImmediate(() => autoUpdater.quitAndInstall());
      } else {
        this.changeUpdaterMenu({ label: 'Updates available', enabled: true });
      }
    });
  }

  public startCheckForUpdatesInterval(): void {
    LOG.info('startCheckForUpdatesInterval called, starting interval...');
    if (this.checkForUpdatesInterval) {
      clearInterval(this.checkForUpdatesInterval);
    }
    this.checkForUpdatesInterval = setInterval(
      () => this.checkForUpdates({ silent: true }),
      30 * 60 * 1000
    );
  }

  public async checkForUpdates({ silent }: { silent: boolean }): Promise<void> {
    if (net.isOnline()) {
      this.isSilentCheckForUpdates = silent;
      this.changeUpdaterMenu({ label: 'Checking for updates...', enabled: false });
      if (this.updateDownloaded) {
        const dialogResponse = await dialog.showMessageBox({
          title: 'PersonalAnalytics Update Available',
          message: 'New updates are available and ready to be installed.',
          defaultId: 0,
          cancelId: 1,
          buttons: ['Install and restart', 'Close']
        });
        if (dialogResponse.response === 0) {
          setImmediate(() => autoUpdater.quitAndInstall());
        } else {
          this.changeUpdaterMenu({ label: 'Updates available', enabled: true });
        }
      } else {
        await autoUpdater.checkForUpdates();
      }
    } else {
      LOG.info('No internet connection, skipping check for updates.');
    }
  }

  private changeUpdaterMenu({ label, enabled }: { label: string; enabled: boolean }): void {
    this.emit('update-tray', { label, enabled });
  }
}
