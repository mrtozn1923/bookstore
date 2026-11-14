import { isValidIsbn13, normalizeIsbn } from './isbn';

describe('isValidIsbn13', () => {
  it('accepts a valid ISBN-13 with hyphens', () => {
    expect(isValidIsbn13('978-0-441-01359-3')).toBe(true);
  });

  it('rejects an ISBN-13 whose check digit is wrong', () => {
    expect(isValidIsbn13('978-0-441-01359-4')).toBe(false);
  });

  it.each([
    ['too short', '978044101359'],
    ['letters', '978044101359X'],
    ['wrong prefix', '9770441013593'],
  ])('rejects input that is %s', (_case, input) => {
    expect(isValidIsbn13(input)).toBe(false);
  });
});

describe('normalizeIsbn', () => {
  it('removes spaces and hyphens', () => {
    expect(normalizeIsbn(' 978-0 441-01359-3 ')).toBe('9780441013593');
  });
});
