import { safeExternalUrl } from './external-url';

describe('safeExternalUrl', () => {
  it('accepts absolute http and https URLs', () => {
    expect(safeExternalUrl('https://example.com/jobs/1?a=b')).toBe(
      'https://example.com/jobs/1?a=b',
    );
    expect(safeExternalUrl('http://example.com/')).toBe('http://example.com/');
  });

  it.each([
    'javascript:alert(1)',
    'data:text/html,<script>',
    '/relative/path',
    'not a url',
    '',
    null,
    undefined,
  ])('rejects %s', (value) => {
    expect(safeExternalUrl(value)).toBeNull();
  });
});
