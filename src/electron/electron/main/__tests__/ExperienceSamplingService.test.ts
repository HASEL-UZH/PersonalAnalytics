import { beforeEach, expect, jest, test } from '@jest/globals';

const where = jest.fn();
const andWhere = jest.fn();
const orderBy = jest.fn();
const getMany = jest.fn();
const createQueryBuilder = jest.fn();

jest.unstable_mockModule('electron', () => ({
  default: {
    app: {
      isPackaged: false
    }
  }
}));

jest.unstable_mockModule('../entities/ExperienceSamplingResponseEntity', () => ({
  ExperienceSamplingResponseEntity: {
    createQueryBuilder
  }
}));

const { ExperienceSamplingService } = await import('../services/ExperienceSamplingService');

beforeEach(() => {
  jest.clearAllMocks();
  getMany.mockResolvedValue([] as never);
  orderBy.mockReturnValue({ getMany });
  andWhere.mockReturnValue({ orderBy });
  where.mockReturnValue({ andWhere });
  createQueryBuilder.mockReturnValue({ where });
});

test('self-reports are queried using the selected workday', async () => {
  const service = new ExperienceSamplingService();
  const workdayRange = {
    start: new Date('2026-06-29T04:00:00.000Z'),
    end: new Date('2026-06-30T04:00:00.000Z')
  };

  await service.getExperienceSamplingDtosForDay(workdayRange);

  expect(createQueryBuilder).toHaveBeenCalledWith('experienceSampling');
  expect(where).toHaveBeenCalledWith('experienceSampling.promptedAt >= :workdayStart', {
    workdayStart: '2026-06-29 04:00:00'
  });
  expect(andWhere).toHaveBeenCalledWith('experienceSampling.promptedAt < :workdayEnd', {
    workdayEnd: '2026-06-30 04:00:00'
  });
});
