// The URL is user input: every query param arrives as a string (or undefined) and may be anything

export type SortOrder = 'title' | 'price';

export function toPage(value: string | undefined): number {
  const page = Number(value);
  return Number.isInteger(page) && page >= 1 ? page : 1;
}

export function toSort(value: string | undefined): SortOrder {
  return value === 'price' ? 'price' : 'title';
}

export function toSearch(value: string | undefined): string {
  return (value ?? '').trim().slice(0, 50);
}
