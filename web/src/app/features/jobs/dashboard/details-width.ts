const STORAGE_KEY = 'swiftjob.detailsPanelWidth';

export const DETAILS_MIN_WIDTH = 340;
/** The job list always keeps at least this much room. */
export const LIST_MIN_WIDTH = 360;

/** Default: roughly half the screen, within sensible bounds. */
export function defaultDetailsWidth(viewportWidth: number): number {
  return Math.round(Math.min(720, Math.max(DETAILS_MIN_WIDTH, viewportWidth * 0.45)));
}

/** Remembered width from a previous visit; browser storage may be unavailable, so failures are ignored. */
export function loadDetailsWidth(): number | null {
  try {
    const value = Number(localStorage.getItem(STORAGE_KEY));
    return Number.isFinite(value) && value > 0 ? value : null;
  } catch {
    return null;
  }
}

export function saveDetailsWidth(width: number | null): void {
  try {
    if (width === null) {
      localStorage.removeItem(STORAGE_KEY);
    } else {
      localStorage.setItem(STORAGE_KEY, String(width));
    }
  } catch {
    // Not critical: the width simply isn't remembered.
  }
}
