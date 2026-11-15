import { useEffect, useEffectEvent, useState } from 'react';
import { BookDetailsPanel } from '@/features/catalog/BookDetailsPanel';
import { BookList } from '@/features/catalog/BookList';
import { SearchBox } from '@/features/catalog/SearchBox';
import type { BookSummary, PagedResult } from '@/features/catalog/types';

type SortBy = 'title' | 'price';

type SearchResult =
  | { status: 'success'; query: string; books: BookSummary[]; total: number }
  | { status: 'error'; query: string; message: string };

type CatalogPageProps = {
  quantities: Record<string, number>;
  cartItemCount: number;
  onAdd: (book: BookSummary, amount: number) => void;
};

const debounceMs = 300;

export function CatalogPage({ quantities, cartItemCount, onAdd }: CatalogPageProps) {
  const [query, setQuery] = useState('');
  const [sortBy, setSortBy] = useState<SortBy>('title');
  const [selectedIsbn, setSelectedIsbn] = useState<string | null>(null);
  const [result, setResult] = useState<SearchResult | null>(null);

  // Not reactive: reads the latest cartItemCount without making the effect re-run
  const onSearchFinished = useEffectEvent((searched: string, total: number) => {
    console.info(`[search] "${searched}" -> ${total} book(s), cart has ${cartItemCount} item(s)`);
  });

  // Synchronizes the result list with the API for the current query
  useEffect(() => {
    const controller = new AbortController();
    const timer = setTimeout(async () => {
      try {
        const url = `/api/books?search=${encodeURIComponent(query.trim())}&pageSize=50`;
        const response = await fetch(url, { signal: controller.signal });
        if (!response.ok) {
          throw new Error(`HTTP ${response.status}`);
        }
        const page = (await response.json()) as PagedResult<BookSummary>;
        setResult({ status: 'success', query, books: page.items, total: page.total });
        onSearchFinished(query, page.total);
      } catch (error) {
        if (controller.signal.aborted) {
          return; // cleanup already ran: this response belongs to an old query
        }
        setResult({ status: 'error', query, message: error instanceof Error ? error.message : String(error) });
      }
    }, debounceMs);

    // Cleanup: runs before the next effect and on unmount
    return () => {
      clearTimeout(timer);
      controller.abort();
    };
  }, [query]);

  // Derived during render: no extra state, no extra effect
  const isLoading = result === null || result.query !== query;
  const books = result?.status === 'success'
    ? result.books.toSorted((a, b) => (sortBy === 'price' ? a.price - b.price : a.title.localeCompare(b.title)))
    : [];
  const selectedBook = books.find((b) => b.isbn === selectedIsbn) ?? null;

  return (
    <>
      <h1>Catalog</h1>
      <div className="toolbar">
        <SearchBox value={query} onChange={setQuery} />
        <label>
          Sort by{' '}
          <select value={sortBy} onChange={(e) => setSortBy(e.target.value as SortBy)}>
            <option value="title">Title</option>
            <option value="price">Price</option>
          </select>
        </label>
      </div>
      <p className="muted" role="status">
        {isLoading ? 'Loading…' : result.status === 'error' ? `Could not load books (${result.message})` : `${result.total} book(s) found`}
      </p>
      {(books.length > 0 || !isLoading) && (
        <BookList books={books} quantities={quantities} onAdd={onAdd} onSelect={setSelectedIsbn} />
      )}
      {selectedBook && <BookDetailsPanel key={selectedBook.isbn} book={selectedBook} />}
    </>
  );
}
