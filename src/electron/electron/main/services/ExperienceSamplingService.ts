import { ExperienceSamplingResponseEntity } from '../entities/ExperienceSamplingResponseEntity';
import getMainLogger from '../../config/Logger';
import ExperienceSamplingDto from '../../../shared/dto/ExperienceSamplingDto';
import type { ExperienceSamplingResponseInput } from '../../../shared/dto/ExperienceSamplingDto';
import type { ExperienceSamplingAnswerType } from '../../../shared/StudyConfiguration';

const LOG = getMainLogger('ExperienceSamplingService');

export class ExperienceSamplingService {
  private toDto(response: ExperienceSamplingResponseEntity): ExperienceSamplingDto {
    return {
      id: response.id,
      question: response.question,
      answerType: response.answerType,
      responseOptions: response.responseOptions,
      scale: response.scale,
      response: response.response,
      promptedAt: response.promptedAt,
      skipped: response.skipped,
      trigger: response.trigger,
      createdAt: response.createdAt,
      updatedAt: response.updatedAt,
      deletedAt: response.deletedAt ?? null
    };
  }

  public async createExperienceSample(
    promptedAt: Date,
    question: string,
    answerType: ExperienceSamplingAnswerType,
    responseOptions: string | null,
    scale: number | null,
    response?: string,
    skipped: boolean,
    trigger: 'manual' | 'auto' = 'auto'
  ): Promise<void> {
    LOG.debug(
      `createExperienceSample: promptedAt=${promptedAt}, question=${question}, response=${response}, skipped=${skipped}, trigger=${trigger}`
    );
    await ExperienceSamplingResponseEntity.save({
      question,
      answerType,
      responseOptions,
      scale,
      response,
      promptedAt,
      skipped,
      trigger
    });
  }

  public async createExperienceSamples(
    promptedAt: Date,
    responses: ExperienceSamplingResponseInput[],
    trigger: 'manual' | 'auto' = 'auto'
  ): Promise<void> {
    LOG.debug(
      `createExperienceSamples: promptedAt=${promptedAt}, responseCount=${responses.length}, trigger=${trigger}`
    );
    const entities = responses.map((r) => {
      const entity = new ExperienceSamplingResponseEntity();
      entity.promptedAt = promptedAt;
      entity.question = r.question;
      entity.answerType = r.answerType;
      entity.responseOptions = r.responseOptions;
      entity.scale = r.scale;
      entity.response = r.response;
      entity.skipped = r.skipped;
      entity.trigger = trigger;
      return entity;
    });
    await ExperienceSamplingResponseEntity.save(entities);
  }

  public async getMostRecentExperienceSamplingDtos(
    itemCount: number
  ): Promise<ExperienceSamplingDto[]> {
    const experienceSamplingResponses = await ExperienceSamplingResponseEntity.find({
      order: { promptedAt: 'DESC' },
      take: itemCount
    });
    return experienceSamplingResponses.map((response) => this.toDto(response));
  }

  public async getExperienceSamplingDtosForDay(
    date: Date | string
  ): Promise<ExperienceSamplingDto[]> {
    const d = typeof date === 'string' ? new Date(date) : date;
    const daystr = d.toISOString().split('T')[0];
    const experienceSamplingResponses = await ExperienceSamplingResponseEntity.createQueryBuilder(
      'experienceSampling'
    )
      .where("date(experienceSampling.promptedAt, 'localtime') = :daystr", { daystr })
      .orderBy('experienceSampling.promptedAt', 'ASC')
      .getMany();

    return experienceSamplingResponses.map((response) => this.toDto(response));
  }
}
