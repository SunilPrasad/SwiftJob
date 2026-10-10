/**
 * Returns the URL only if it is an absolute http(s) URL. Job data comes from untrusted emails,
 * so anything else (javascript:, data:, relative paths) is never turned into a link.
 */
export function safeExternalUrl(url: string | null | undefined): string | null {
  if (!url) {
    return null;
  }
  try {
    const parsed = new URL(url);
    return parsed.protocol === 'https:' || parsed.protocol === 'http:' ? parsed.href : null;
  } catch {
    return null;
  }
}
