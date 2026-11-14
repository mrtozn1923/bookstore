export function normalizeIsbn(value: string): string {
  return value.replace(/[\s-]/g, '');
}

export function isValidIsbn13(value: string): boolean {
  const isbn = normalizeIsbn(value);
  if (!/^97[89]\d{10}$/.test(isbn)) {
    return false;
  }

  const sum = [...isbn.slice(0, 12)]
    .map(Number)
    .reduce((total, digit, index) => total + digit * (index % 2 === 0 ? 1 : 3), 0);
  const checkDigit = (10 - (sum % 10)) % 10;
  return checkDigit === Number(isbn[12]);
}
