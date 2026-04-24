import { DailySurveyResponseEntity } from '../entities/DailySurveyResponseEntity';
import getMainLogger from '../../config/Logger';
import DailySurveyDto from '../../../shared/dto/DailySurveyDto';
import type { DailySurveySamplingType, ExperienceSamplingAnswerType } from '../../../shared/StudyConfiguration';

const LOG = getMainLogger('DailySurveyService');

export interface DailySurveyResponseInput {
  question: string;
  answerType: ExperienceSamplingAnswerType;
  responseOptions: string | null;
  scale: number | null;
  response: string | null;
  skipped: boolean;
}

export class DailySurveyService {
  public async createDailySurveyResponses(
    promptedAt: Date,
    samplingType: DailySurveySamplingType,
    responses: DailySurveyResponseInput[]
  ): Promise<void> {
    LOG.debug(
      `createDailySurveyResponses: promptedAt=${promptedAt}, samplingType=${samplingType}, responseCount=${responses.length}`
    );
    const entities = responses.map((r) => {
      const entity = new DailySurveyResponseEntity();
      entity.promptedAt = promptedAt;
      entity.samplingType = samplingType;
      entity.question = r.question;
      entity.answerType = r.answerType;
      entity.responseOptions = r.responseOptions;
      entity.scale = r.scale;
      entity.response = r.response;
      entity.skipped = r.skipped;
      return entity;
    });
    await DailySurveyResponseEntity.save(entities);
  }

  public async getMostRecentDailySurveyDtos(
    itemCount: number
  ): Promise<DailySurveyDto[]> {
    const responses = await DailySurveyResponseEntity.find({
      order: { promptedAt: 'DESC' },
      take: itemCount
    });
    return responses.map((r) => ({
      id: r.id,
      promptedAt: r.promptedAt,
      samplingType: r.samplingType,
      question: r.question,
      answerType: r.answerType,
      responseOptions: r.responseOptions,
      scale: r.scale,
      response: r.response,
      skipped: r.skipped,
      createdAt: r.createdAt,
      updatedAt: r.updatedAt,
      deletedAt: r.deletedAt
    }));
  }
}
