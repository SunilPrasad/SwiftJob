/** Screen area for the SwiftJob browser window, in screen pixels. */
export interface WindowPlacement {
  x: number;
  y: number;
  width: number;
  height: number;
}

/**
 * The right half of the screen that SwiftJob is shown on, so the browser opens side by side with it.
 * (Snap SwiftJob to the left half, e.g. Windows key + Left arrow.)
 */
export function rightHalfOfScreen(screen: Screen = window.screen): WindowPlacement {
  const s = screen as Screen & { availLeft?: number; availTop?: number };
  const left = s.availLeft ?? 0;
  const top = s.availTop ?? 0;
  const half = Math.floor(s.availWidth / 2);
  return {
    x: left + half,
    y: top,
    width: Math.max(400, s.availWidth - half),
    height: Math.max(300, s.availHeight),
  };
}
