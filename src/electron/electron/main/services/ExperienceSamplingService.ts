import { ExperienceSamplingResponseEntity } from '../entities/ExperienceSamplingResponseEntity';

export class ExperienceSamplingService {
  public async createExperienceSample(promptedAt: number, question: string, response: number) {
    await ExperienceSamplingResponseEntity.save({
      question,
      response,
      promptedAt
    });
  }
}
