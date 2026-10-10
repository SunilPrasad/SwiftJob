import { toUtcRange } from './date-range';

describe('toUtcRange', () => {
  it('returns local midnight of the start day and of the day after the end day', () => {
    const range = toUtcRange(new Date(2026, 9, 1, 15, 30), new Date(2026, 9, 3, 9, 0));

    expect(range.receivedFrom).toBe(new Date(2026, 9, 1).toISOString());
    expect(range.receivedBefore).toBe(new Date(2026, 9, 4).toISOString());
  });

  it('returns nulls for an empty range', () => {
    expect(toUtcRange(null, null)).toEqual({ receivedFrom: null, receivedBefore: null });
  });
});
