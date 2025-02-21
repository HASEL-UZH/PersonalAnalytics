import { app, BrowserWindow, clipboard, dialog, Menu, nativeImage, screen, shell, Tray } from 'electron'
import getMainLogger from '../../config/Logger'
import AppUpdaterService from './AppUpdaterService'
import { is } from './utils/helpers'
import path from 'path'
import MenuItemConstructorOptions = Electron.MenuItemConstructorOptions
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'
import studyConfig from '../../../shared/study.config'

import { UsageDataService } from './UsageDataService'
import { UsageDataEventType } from '../../enums/UsageDataEventType.enum'
import { Settings } from '../entities/Settings'

const LOG = getMainLogger('WindowService')

export class WindowService {
  private readonly appUpdaterService: AppUpdaterService
  private tray: Tray
  private experienceSamplingWindow: BrowserWindow
  private onboardingWindow: BrowserWindow
  private dataExportWindow: BrowserWindow
  private settingsWindow: BrowserWindow

  private hasOpenedDataExportUrl: boolean = false;
  private hasRevealedDataEportFolder: boolean = false;

  constructor(appUpdaterService: AppUpdaterService) {
    LOG.debug('WindowService constructor called')

    this.appUpdaterService = appUpdaterService
    this.appUpdaterService.on(
      'update-tray',
      ({ label, enabled }: { label: string; enabled: boolean }) => {
        this.updateTray(label, enabled)
      }
    )
    if (!is.dev) {
      Menu.setApplicationMenu(null)
    }
  }

  public async init(): Promise<void> {
    await this.createTray()
  }

  public async createExperienceSamplingWindow(isManuallyTriggered: boolean = false) {
    if (this.experienceSamplingWindow) {
      this.experienceSamplingWindow.close()
      this.experienceSamplingWindow = null
    }

    const usageDataEvent = isManuallyTriggered
      ? UsageDataEventType.ExperienceSamplingManuallyOpened
      : UsageDataEventType.ExperienceSamplingAutomaticallyOpened
    UsageDataService.createNewUsageDataEvent(usageDataEvent)

    const __filename = fileURLToPath(import.meta.url)
    const __dirname = dirname(__filename)
    const preload = join(__dirname, '../preload/index.mjs')

    const { width } = screen.getPrimaryDisplay().workAreaSize
    const windowPadding = 20
    const windowWidth = 500
    const windowHeight = 170

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
    })

    if (process.env.VITE_DEV_SERVER_URL) {
      await this.experienceSamplingWindow.loadURL(
        process.env.VITE_DEV_SERVER_URL + '#experience-sampling'
      )
    } else {
      await this.experienceSamplingWindow.loadFile(path.join(process.env.DIST, 'index.html'), {
        hash: 'experience-sampling'
      })
    }

    this.experienceSamplingWindow.setVisibleOnAllWorkspaces(true)
    let opacity = 0
    const interval = setInterval(() => {
      if (opacity >= 1) clearInterval(interval)
      this.experienceSamplingWindow?.setOpacity(opacity)
      opacity += 0.1
    }, 10)
    this.experienceSamplingWindow.showInactive()

    this.experienceSamplingWindow.on('close', () => {
      this.experienceSamplingWindow = null
    })
  }

  public closeExperienceSamplingWindow(skippedExperienceSampling: boolean) {
    const usageDataEvent = skippedExperienceSampling
      ? UsageDataEventType.ExperienceSamplingSkipped
      : UsageDataEventType.ExperienceSamplingAnswered
    UsageDataService.createNewUsageDataEvent(usageDataEvent)

    if (this.experienceSamplingWindow) {
      this.experienceSamplingWindow.close()
      this.experienceSamplingWindow = null
    }
  }

  public async closeSettingsWindow() {
    if (this.settingsWindow) {
      this.settingsWindow?.close()
      this.settingsWindow = null
    }
  }

  public async createSettingsWindow() {
    this.closeSettingsWindow()

    const __filename = fileURLToPath(import.meta.url)
    const __dirname = dirname(__filename)
    const preload = join(__dirname, '../preload/index.mjs')
    this.settingsWindow = new BrowserWindow({
      width: 1000,
      height: 850,
      show: false,
      minimizable: false,
      maximizable: false,
      fullscreenable: false,
      resizable: false,
      title: 'PersonalAnalytics: Settings',
      webPreferences: {
        preload
      }
    })

    if (process.env.VITE_DEV_SERVER_URL) {
      await this.settingsWindow.loadURL(process.env.VITE_DEV_SERVER_URL + `#settings?isMacOS=${is.macOS}`)
    } else {
      await this.settingsWindow.loadFile(path.join(process.env.DIST, 'index.html'), {
        hash: `settings?isMacOS=${is.macOS}`
      })
    }

    this.settingsWindow.webContents.setWindowOpenHandler((details) => {
      shell.openExternal(details.url)
      return { action: 'deny' }
    })

    this.settingsWindow.show()

    this.settingsWindow.on('close', () => {
      this.settingsWindow = null
    })
  }

  public closeOnboardingWindow() {
    if (this.onboardingWindow) {
      this.onboardingWindow?.close()
      this.onboardingWindow = null
    }
  }

  public async createOnboardingWindow(goToStep?: string) {
    this.closeOnboardingWindow()

    const __filename = fileURLToPath(import.meta.url)
    const __dirname = dirname(__filename)
    const preload = join(__dirname, '../preload/index.mjs')
    this.onboardingWindow = new BrowserWindow({
      width: 800,
      height: 850,
      show: false,
      minimizable: false,
      maximizable: false,
      fullscreenable: false,
      resizable: false,
      title: 'PersonalAnalytics: Onboarding',
      webPreferences: {
        preload
      }
    })

    if (process.env.VITE_DEV_SERVER_URL) {
      await this.onboardingWindow.loadURL(
        process.env.VITE_DEV_SERVER_URL +
        `#onboarding?isMacOS=${is.macOS}&goToStep=${goToStep ?? 'welcome'}`
      )
    } else {
      await this.onboardingWindow.loadFile(path.join(process.env.DIST, 'index.html'), {
        hash: `onboarding?isMacOS=${is.macOS}&goToStep=${goToStep ?? 'welcome'}`
      })
    }

    this.onboardingWindow.webContents.setWindowOpenHandler((details) => {
      shell.openExternal(details.url)
      return { action: 'deny' }
    })

    this.onboardingWindow.show()

    this.onboardingWindow.on('close', () => {
      this.onboardingWindow = null
    })
  }

  public closeDataExportWindow() {
    if (this.dataExportWindow) {
      this.dataExportWindow.close()
    }
  }

  private destroyDataExportWindow() {
    if (this.dataExportWindow) {
      this.dataExportWindow.destroy()
      this.dataExportWindow = null
    }

    if (is.macOS) {
      app.dock.hide()
    }
  }

  public async createDataExportWindow() {
    this.destroyDataExportWindow()

    const __filename = fileURLToPath(import.meta.url)
    const __dirname = dirname(__filename)
    const preload = join(__dirname, '../preload/index.mjs')
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
    })

    if (process.env.VITE_DEV_SERVER_URL) {
      await this.dataExportWindow.loadURL(process.env.VITE_DEV_SERVER_URL + `#data-export`)
    } else {
      await this.dataExportWindow.loadFile(path.join(process.env.DIST, 'index.html'), {
        hash: `data-export?isMacOS`
      })
    }

    this.dataExportWindow.webContents.setWindowOpenHandler((details) => {
      shell.openExternal(details.url)
      return { action: 'deny' }
    })

    if (is.macOS && !is.dev) {
      const template = [
        {
          label: 'Edit',
          submenu: [
            { label: 'Copy', accelerator: 'CmdOrCtrl+C', selector: 'copy:' },
            { label: 'Paste', accelerator: 'CmdOrCtrl+V', selector: 'paste:' }
          ]
        }
      ]
      Menu.setApplicationMenu(Menu.buildFromTemplate(template))
    }

    if (is.macOS) {
      await app.dock.show()
    }

    this.dataExportWindow.show()

    this.dataExportWindow.on('close', async (event) => {
      event.preventDefault() // prevents the window from closing 

      const seemsToHaveCompletedExport = this.hasOpenedDataExportUrl && this.hasRevealedDataEportFolder
      if (!seemsToHaveCompletedExport) {
        const result = await dialog.showMessageBox({
          type: 'warning',
          buttons: ['Continue with data export', 'Close Anyway'],
          defaultId: 0,
          cancelId: 0,
          title: 'Complete Data Export',
          message: 'It seems that you have not completed the data export process.',
          detail: 'Please make sure you completed all the steps in this window, including manually uploading the exported data file. Do you want to close the window anyway?'
        })

        if (result.response === 0) {
          LOG.info('User chose to continue with data export, return...')
          return
        }
      }

      LOG.info('User chose to close data export window, closing...')
      this.destroyDataExportWindow()

      // reset flags for next time
      this.hasOpenedDataExportUrl = false
      this.hasRevealedDataEportFolder = false
    })
  }

  public showItemInFolder(path: string): void {
    this.hasRevealedDataEportFolder = true
    shell.showItemInFolder(path)
  }

  public async openExternal(): Promise<void> {
    this.hasOpenedDataExportUrl = true
    shell.openExternal(studyConfig.uploadUrl)
  }

  public async updateTray(
    updaterLabel: string = 'Check for updates',
    updaterMenuEnabled: boolean = false
  ): Promise<void> {
    LOG.debug('Updating tray')
    const menuTemplate: MenuItemConstructorOptions[] = await this.getTrayMenuTemplate()
    menuTemplate[1].label = updaterLabel
    menuTemplate[1].enabled = updaterMenuEnabled

    this.tray.setContextMenu(Menu.buildFromTemplate(menuTemplate))
    this.tray.on("click", () => { this.tray.popUpContextMenu() })
    this.tray.setToolTip(`Personal Analytics is running...\n\nYou are participating in: ${studyConfig.name}`)
  }

  private async createTray(): Promise<void> {
    LOG.debug('Creating tray')
    if (this.tray) {
      return
    }
    const iconToUse = is.macOS ? 'IconTemplate.png' : 'IconColored@2x.png'
    const appIcon = path.join(process.env.VITE_PUBLIC, iconToUse)
    const trayImage = nativeImage.createFromPath(appIcon)
    trayImage.setTemplateImage(true)
    this.tray = new Tray(trayImage)
    await this.updateTray()
  }

  private async getTrayMenuTemplate(): Promise<MenuItemConstructorOptions[]> {
    const settings: Settings = await Settings.findOne({ where: { onlyOneEntityShouldExist: 1 } })
    const trayMenuItems: MenuItemConstructorOptions[] = [
      { label: `Version ${app.getVersion()}`, enabled: false },
      {
        label: 'Check for updates',
        enabled: false,
        click: () => this.appUpdaterService.checkForUpdates({ silent: false })
      },
      { type: 'separator' },
      {
        label: `Subject ID: ${settings.subjectId}`,
        enabled: false,
      },
      {
        label: 'Copy Subject Id',
        click: () => clipboard.writeText(settings.subjectId)
      },
      {
        label: `Days participated: ${settings.daysParticipated}`,
        enabled: false,
        visible: studyConfig.displayDaysParticipated
      },
      { type: 'separator' },
      {
        label: 'Open Experience Sampling',
        click: () => this.createExperienceSamplingWindow(true)
      },
      {
        label: 'Open Settings',
        click: () => this.createSettingsWindow()
      },
      {
        label: 'Open Onboarding',
        click: () => this.createOnboardingWindow(),
        visible: is.dev
      },
      {
        label: 'Open Study Data Export',
        click: (): void => {
          LOG.info(`Opening data export`)
          this.createDataExportWindow()
        },
        visible: studyConfig.dataExportEnabled
      },
      { type: 'separator' },
      {
        label: 'Get Help',
        click: (): void => {
          const mailToAddress = studyConfig.contactEmail
          shell.openExternal(`mailto:${mailToAddress}`)
        }
      },
      {
        label: 'Report a Problem',
        click: (): void => {
          const mailToAddress = studyConfig.contactEmail
          shell.openExternal(`mailto:${mailToAddress}`)
        }
      },
      { type: 'separator' },
      {
        label: 'Quit',
        click: () => {
          app.quit()
        }
      }
    ]

    return trayMenuItems
  }
}
