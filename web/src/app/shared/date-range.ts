/**
 * Converts a local calendar date range (as picked in the UI) to UTC instants:
 * an inclusive start at local midnight and an exclusive end at the following local midnight.
 */
export function toUtcRange(
  start: Date | null,
  end: Date | null,
): { receivedFrom: string | null; receivedBefore: string | null } {
  const receivedFrom = start
    ? new Date(start.getFullYear(), start.getMonth(), start.getDate()).toISOString()
    : null;
  const receivedBefore = end
    ? new Date(end.getFullYear(), end.getMonth(), end.getDate() + 1).toISOString()
    : null;
  return { receivedFrom, receivedBefore };
}
