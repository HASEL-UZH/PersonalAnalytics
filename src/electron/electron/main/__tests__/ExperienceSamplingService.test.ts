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

test('self-reports are queried using the selected 04:00-to-04:00 local workday', async () => {
  const service = new ExperienceSamplingService();

  await service.getExperienceSamplingDtosForDay(new Date(2026, 5, 29, 0, 0));

  expect(createQueryBuilder).toHaveBeenCalledWith('experienceSampling');
  expect(where).toHaveBeenCalledWith(
    "datetime(experienceSampling.promptedAt, 'localtime') >= :workdayStart",
    { workdayStart: '2026-06-29 04:00:00' }
  );
  expect(andWhere).toHaveBeenCalledWith(
    "datetime(experienceSampling.promptedAt, 'localtime') < :workdayEnd",
    { workdayEnd: '2026-06-30 04:00:00' }
  );
});

test('date-only self-report queries do not pass through UTC date conversion', async () => {
  const service = new ExperienceSamplingService();

  await service.getExperienceSamplingDtosForDay('2026-06-29');

  expect(where).toHaveBeenCalledWith(expect.any(String), {
    workdayStart: '2026-06-29 04:00:00'
  });
  expect(andWhere).toHaveBeenCalledWith(expect.any(String), {
    workdayEnd: '2026-06-30 04:00:00'
  });
});
