import { afterEach, expect, jest, test } from '@jest/globals';
import { UserInputEntity } from '../entities/UserInputEntity';
import { WindowActivityEntity } from '../entities/WindowActivityEntity';
import { loadRetrospectionWorkdayData } from '../services/retrospection/RetrospectionWorkdayData';

interface MockQueryBuilder {
  select: ReturnType<typeof jest.fn>;
  where: ReturnType<typeof jest.fn>;
  andWhere: ReturnType<typeof jest.fn>;
  orderBy: ReturnType<typeof jest.fn>;
  getRawMany: ReturnType<typeof jest.fn>;
}

function createQueryBuilder(rows: unknown[]): MockQueryBuilder {
  const builder = {} as MockQueryBuilder;
  builder.select = jest.fn(() => builder);
  builder.where = jest.fn(() => builder);
  builder.andWhere = jest.fn(() => builder);
  builder.orderBy = jest.fn(() => builder);
  builder.getRawMany = jest.fn(async () => rows);
  return builder;
}

afterEach(() => {
  jest.restoreAllMocks();
});

test('loads each raw tracker source once for the whole workday', async () => {
  const windowBuilder = createQueryBuilder([
    {
      activity: 'DevCode',
      processName: 'Code',
      processPath: null,
      ts: '2026-07-14 09:00:00',
      url: null,
      windowTitle: 'main.ts - Code'
    }
  ]);
  const inputBuilder = createQueryBuilder([
    {
      clickTotal: 1,
      keysTotal: 0,
      movedDistance: 0,
      scrollDelta: 0,
      tsStart: '2026-07-14 09:00:00'
    }
  ]);
  const windowQuery = jest
    .spyOn(WindowActivityEntity, 'createQueryBuilder')
    .mockReturnValue(windowBuilder as never);
  const inputQuery = jest
    .spyOn(UserInputEntity, 'createQueryBuilder')
    .mockReturnValue(inputBuilder as never);

  const workdayData = await loadRetrospectionWorkdayData(new Date(2026, 6, 14));

  expect(windowQuery).toHaveBeenCalledTimes(1);
  expect(inputQuery).toHaveBeenCalledTimes(1);
  expect(workdayData.windowActivities).toHaveLength(1);
  expect(workdayData.windowActivities[0].ts).toBeInstanceOf(Date);
  expect(workdayData.activeMinutes).toEqual(new Set([5 * 60]));
  expect(workdayData.workdayStart).toEqual(new Date(2026, 6, 14, 4));
});
