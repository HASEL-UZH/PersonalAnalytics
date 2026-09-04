import { describe, expect, test } from '@jest/globals';
import { formatDuration } from '../utils';

describe('formatDuration', () => {
  test.each([
    [0, '0 mins'],
    [20_000, '< 1 min'],
    [60_000, '1 min'],
    [24 * 60_000, '24 mins'],
    [60 * 60_000, '1 hr'],
    [61 * 60_000, '1 hr 1 min'],
    [84 * 60_000, '1 hr 24 mins'],
    [120 * 60_000, '2 hrs']
  ])('formats %s milliseconds as %s', (durationInMs, expected) => {
    expect(formatDuration(durationInMs)).toBe(expected);
  });
});
