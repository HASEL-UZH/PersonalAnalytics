import { ExperienceSamplingResponseEntity } from '../entities/ExperienceSamplingResponseEntity';
import { getLogger } from '../../shared/Logger';

const LOG = getLogger('ExperienceSamplingService');

export class ExperienceSamplingService {
  public async createExperienceSample(promptedAt: number, question: string, response: number) {
    LOG.debug(
      `createExperienceSample: promptedAt=${promptedAt}, question=${question}, response=${response}`
    );
    await ExperienceSamplingResponseEntity.save({
      question,
      response,
      promptedAt
    });
  }
}
