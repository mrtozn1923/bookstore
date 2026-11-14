// Modelin seçebileceği component'ler ve props şemaları; listede olmayan hiçbir şey render edilmez
export interface BookCardData {
  isbn: string;
  title: string;
  author: string;
  priceTry: number;
}

export type UiBlock = { component: 'book-list'; heading: string; books: BookCardData[] };

const isText = (v: unknown, max: number): v is string => typeof v === 'string' && v.length > 0 && v.length <= max;

function parseBook(v: unknown): BookCardData | null {
  if (typeof v !== 'object' || v === null) {
    return null;
  }
  const b = v as Record<string, unknown>;
  return /^\d{13}$/.test(String(b['isbn'])) && isText(b['title'], 200) && isText(b['author'], 200) &&
    typeof b['priceTry'] === 'number' && b['priceTry'] >= 0
    ? { isbn: String(b['isbn']), title: b['title'], author: b['author'], priceTry: b['priceTry'] }
    : null;
}

// Sunucudan gelen ui event'ini doğrular; şemaya uymayan blok atılır
export function parseUiBlock(component: string, props: unknown): UiBlock | null {
  if (component !== 'book-list' || typeof props !== 'object' || props === null) {
    return null;
  }
  const p = props as Record<string, unknown>;
  if (!isText(p['heading'], 120) || !Array.isArray(p['books']) || p['books'].length > 12) {
    return null;
  }
  const books = p['books'].map(parseBook);
  return books.every((b) => b !== null) ? { component: 'book-list', heading: p['heading'], books } : null;
}
