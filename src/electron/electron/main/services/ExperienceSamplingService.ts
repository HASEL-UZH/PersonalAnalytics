import { ExperienceSamplingResponseEntity } from '../entities/ExperienceSamplingResponseEntity';
import { getLogger } from '../../shared/Logger';
import ExperienceSamplingDto from '../../../shared/dto/ExperienceSamplingDto';

const LOG = getLogger('ExperienceSamplingService');

export class ExperienceSamplingService {
  public async createExperienceSample(
    promptedAt: Date,
    question: string,
    responseOptions: string,
    scale: number,
    response: number,
    skipped: boolean
  ): Promise<void> {
    LOG.debug(
      `createExperienceSample: promptedAt=${promptedAt}, question=${question}, response=${response}, skipped=${skipped}`
    );
    await ExperienceSamplingResponseEntity.save({
      question,
      responseOptions,
      scale,
      response,
      promptedAt,
      skipped
    });
  }

  public async getMostRecentExperienceSamplingDtos(
    itemCount: number
  ): Promise<ExperienceSamplingDto[]> {
    const experienceSamplingResponses = await ExperienceSamplingResponseEntity.find({
      order: { promptedAt: 'DESC' },
      take: itemCount
    });
    return experienceSamplingResponses.map((response) => ({
      id: response.id,
      question: response.question,
      responseOptions: response.responseOptions,
      scale: response.scale,
      response: response.response,
      promptedAt: response.promptedAt,
      skipped: response.skipped,
      createdAt: response.createdAt,
      updatedAt: response.updatedAt,
      deletedAt: response.deletedAt
    }));
  }
}
