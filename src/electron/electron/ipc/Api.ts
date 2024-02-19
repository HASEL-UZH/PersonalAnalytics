import { TypedIpcMain } from './TypedIpcMain';
import Events from '../shared/Events';
import Commands from '../shared/Commands';
import { ExperienceSamplingService } from '../main/services/ExperienceSamplingService';
import { ipcMain, IpcMainInvokeEvent } from 'electron';
import { WindowService } from '../main/services/WindowService';
import { getLogger } from '../shared/Logger';


const LOG = getLogger('IpcHandler')
export class IpcHandler {
  private readonly actions: any;
  private readonly windowService: WindowService;

  private readonly experienceSamplingService: ExperienceSamplingService;
  private typedIpcMain: TypedIpcMain<Events, Commands> = ipcMain as TypedIpcMain<Events, Commands>;

  constructor(windowService: WindowService, experienceSamplingService: ExperienceSamplingService) {
    this.windowService = windowService;
    this.experienceSamplingService = experienceSamplingService;
    this.actions = {
      createExperienceSample: this.createExperienceSample,
      closeExperienceSamplingWindow: this.closeExperienceSamplingWindow
    };
  }

  public init(): void {
    Object.keys(this.actions).forEach((action: string): void => {
      LOG.info(`ipcMain.handle setup: ${action}`);
      ipcMain.handle(action, async (_event: IpcMainInvokeEvent, ...args): Promise<any> => {
        try {
          return await this.actions[action].apply(this, args);
        } catch (error) {
          LOG.error(error);
          return error;
        }
      });
    });
  }

  private async createExperienceSample(promptedAt: number, question: string, response: number) {
    await this.experienceSamplingService.createExperienceSample(promptedAt, question, response);
  }

  private async closeExperienceSamplingWindow(): Promise<void> {
    await this.windowService.closeExperienceSamplingWindow();
  }
}
