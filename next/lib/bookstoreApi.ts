import 'server-only';
import { cacheLife, cacheTag } from 'next/cache';

// Read on the server only; never sent to the browser (no NEXT_PUBLIC_ prefix)
const apiBase = process.env.BOOKSTORE_API ?? 'http://localhost:5080';

export type Book = {
  isbn: string;
  title: string;
  author: string;
  category: string;
  price: number;
  publishedYear: number;
  description: string;
};

type BookWithStock = Book & { available: number };

async function getJson<T>(path: string, caller: string): Promise<T | null> {
  console.log(`[api] ${caller}: GET ${path}`);
  const response = await fetch(`${apiBase}${path}`);
  if (response.status === 404) {
    return null;
  }
  if (!response.ok) {
    throw new Error(`Bookstore.Api answered ${response.status} for ${path}`);
  }
  return (await response.json()) as T;
}

// Cached on the server: the same ISBN does not reach Bookstore.Api again for an hour
export async function getBook(isbn: string): Promise<Book | null> {
  'use cache';
  cacheLife('hours');
  cacheTag(`book-${isbn}`);
  const book = await getJson<BookWithStock>(`/api/books/${isbn}`, 'getBook');
  if (book === null) {
    return null;
  }
  // The stock figure must not be cached for an hour: leave it out of the cached value
  const { available: _, ...rest } = book;
  return rest;
}

export async function getBooksByAuthor(author: string): Promise<{ isbn: string; title: string }[]> {
  'use cache';
  cacheLife('hours');
  const result = await getJson<{ items: Book[] }>(`/api/books?search=${encodeURIComponent(author)}&pageSize=20`, 'getBooksByAuthor');
  return (result?.items ?? []).filter((b) => b.author === author).map(({ isbn, title }) => ({ isbn, title }));
}

export async function getCatalogPage(): Promise<Book[]> {
  'use cache';
  cacheLife('minutes');
  return (await getJson<{ items: Book[] }>('/api/books?page=1&pageSize=8', 'getCatalogPage'))?.items ?? [];
}

// Not cached: stock changes with every order, so it is read on every request
export async function getStock(isbn: string): Promise<number> {
  return (await getJson<BookWithStock>(`/api/books/${isbn}`, 'getStock'))?.available ?? 0;
}
