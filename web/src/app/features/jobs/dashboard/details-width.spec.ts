import {
  DETAILS_MIN_WIDTH,
  defaultDetailsWidth,
  loadDetailsWidth,
  saveDetailsWidth,
} from './details-width';

describe('details panel width', () => {
  afterEach(() => saveDetailsWidth(null));

  it('defaults to about half the screen within bounds', () => {
    expect(defaultDetailsWidth(1400)).toBe(630);
    expect(defaultDetailsWidth(3000)).toBe(720);
    expect(defaultDetailsWidth(500)).toBe(DETAILS_MIN_WIDTH);
  });

  it('remembers and forgets the chosen width', () => {
    expect(loadDetailsWidth()).toBeNull();
    saveDetailsWidth(512);
    expect(loadDetailsWidth()).toBe(512);
    saveDetailsWidth(null);
    expect(loadDetailsWidth()).toBeNull();
  });
});
