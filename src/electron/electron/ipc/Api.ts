import { TypedIpcMain } from './TypedIpcMain';
import Events from '../shared/Events';
import Commands from '../shared/Commands';
import { ExperienceSamplingService } from '../main/services/ExperienceSamplingService';
import { ipcMain } from 'electron';

const typedIpcMain: TypedIpcMain<Events, Commands> = ipcMain as TypedIpcMain<Events, Commands>;
const experienceSamplingService: ExperienceSamplingService = new ExperienceSamplingService();
typedIpcMain.handle(
  'createExperienceSample',
  async (e, promptedAt: number, question: string, response: number): Promise<void> => {
    await experienceSamplingService.createExperienceSample(promptedAt, question, response);
  }
);
