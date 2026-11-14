// Typo-tolerant search over the catalog. Pure functions: the same code runs in the worker and on the main thread.
export interface CatalogEntry {
  isbn: string;
  title: string;
  author: string;
  subjects: string;
}

export interface SearchHit {
  isbn: string;
  title: string;
  author: string;
  typos: number;
}

// what the worker exposes through Comlink; kept here so the app never imports the worker file
export interface CatalogSearchApi {
  load(url: string): Promise<number>;
  search(query: string): SearchHit[];
}

export interface IndexedEntry {
  entry: CatalogEntry;
  words: string[];
}

const normalize = (text: string) =>
  text
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase();

export function buildIndex(entries: CatalogEntry[]): IndexedEntry[] {
  return entries.map((entry) => ({
    entry,
    words: normalize(`${entry.title} ${entry.author} ${entry.subjects}`)
      .split(/[^\p{L}\p{N}]+/u)
      .filter(Boolean),
  }));
}

export function terms(query: string): string[] {
  return normalize(query).split(/\s+/).filter(Boolean);
}

// every term must match the start of some word with at most 0, 1 or 2 typos, depending on its length
export function searchSlice(items: IndexedEntry[], queryTerms: string[]): SearchHit[] {
  const hits: SearchHit[] = [];
  for (const { entry, words } of items) {
    let typos = 0;
    for (const term of queryTerms) {
      let best = Infinity;
      for (const word of words) {
        for (let n = term.length - 1; n <= term.length + 1; n++) {
          best = Math.min(best, distance(term, word.slice(0, n)));
        }
      }
      if (best > allowedTypos(term)) {
        typos = -1;
        break;
      }
      typos += best;
    }
    if (typos >= 0)
      hits.push({ isbn: entry.isbn, title: entry.title, author: entry.author, typos });
  }
  return hits;
}

export function rank(hits: SearchHit[], limit = 20): SearchHit[] {
  return hits.sort((a, b) => a.typos - b.typos || a.title.localeCompare(b.title)).slice(0, limit);
}

export function search(index: IndexedEntry[], query: string, limit = 20): SearchHit[] {
  const queryTerms = terms(query);
  return queryTerms.length ? rank(searchSlice(index, queryTerms), limit) : [];
}

function allowedTypos(term: string) {
  return term.length <= 3 ? 0 : term.length <= 6 ? 1 : 2;
}

// Levenshtein distance with two rows
function distance(a: string, b: string): number {
  let previous = Array.from({ length: b.length + 1 }, (_, i) => i);
  for (let i = 1; i <= a.length; i++) {
    const current = [i];
    for (let j = 1; j <= b.length; j++) {
      const cost = a[i - 1] === b[j - 1] ? 0 : 1;
      current[j] = Math.min(previous[j] + 1, current[j - 1] + 1, previous[j - 1] + cost);
    }
    previous = current;
  }
  return previous[b.length];
}
