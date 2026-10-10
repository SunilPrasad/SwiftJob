import { rightHalfOfScreen } from './window-placement';

describe('rightHalfOfScreen', () => {
  it('returns the right half of the available screen area', () => {
    const screen = {
      availLeft: 0,
      availTop: 0,
      availWidth: 1920,
      availHeight: 1040,
    } as unknown as Screen;

    expect(rightHalfOfScreen(screen)).toEqual({ x: 960, y: 0, width: 960, height: 1040 });
  });

  it('respects a second monitor offset', () => {
    const screen = {
      availLeft: 1920,
      availTop: 0,
      availWidth: 2560,
      availHeight: 1400,
    } as unknown as Screen;

    expect(rightHalfOfScreen(screen)).toEqual({ x: 3200, y: 0, width: 1280, height: 1400 });
  });
});
