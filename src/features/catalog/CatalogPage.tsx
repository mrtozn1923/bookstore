import { useQuery } from '@tanstack/react-query';
import { Suspense, useRef, useState } from 'react';
import { useCart } from '@/features/cart/useCart';
import { BookDetailsPanel } from '@/features/catalog/BookDetailsPanel';
import { BookList } from '@/features/catalog/BookList';
import { booksQuery, pageSize } from '@/features/catalog/catalogApi';
import { SearchBox } from '@/features/catalog/SearchBox';
import { ErrorBoundary } from '@/shared/ErrorBoundary';
import { useDebouncedValue } from '@/shared/useDebouncedValue';
import { useKeyboardShortcut } from '@/shared/useKeyboardShortcut';

export function CatalogPage() {
  const [query, setQuery] = useState('');
  const [page, setPage] = useState(1);
  const [selectedIsbn, setSelectedIsbn] = useState<string | null>(null);
  const { quantityOf, add } = useCart();
  const searchInput = useRef<HTMLInputElement>(null);
  useKeyboardShortcut('/', () => searchInput.current?.focus());
  useKeyboardShortcut('Escape', () => setSelectedIsbn(null));

  // Replaces the hand-written effect: caching, deduplication, cancellation and retries come with it
  const search = useDebouncedValue(query.trim(), 300);
  const { data, error, isFetching, isPlaceholderData } = useQuery(booksQuery(search, page));
  const pageCount = data ? Math.max(1, Math.ceil(data.total / pageSize)) : 1;

  function handleQueryChange(value: string) {
    setQuery(value);
    setPage(1); // a new search starts on the first page
  }

  return (
    <>
      <h1>Catalog</h1>
      <div className="toolbar">
        <SearchBox ref={searchInput} value={query} onChange={handleQueryChange} />
      </div>
      <p className="muted" role="status">
        {error ? `Could not load books (${error.message})`
          : !data ? 'Loading…'
          : `${data.total} book(s) found${isFetching ? ' · updating…' : ''}`}
      </p>
      {data && (
        <>
          <div className={isPlaceholderData ? 'stale' : undefined}>
            <BookList books={data.items} quantityOf={quantityOf} onAdd={add} onSelect={setSelectedIsbn} />
          </div>
          <nav className="pager" aria-label="Pages">
            <button type="button" disabled={page === 1} onClick={() => setPage(page - 1)}>Previous</button>
            <span>Page {page} of {pageCount}</span>
            <button type="button" disabled={page >= pageCount} onClick={() => setPage(page + 1)}>Next</button>
          </nav>
        </>
      )}
      {selectedIsbn && (
        // key: another book gets a fresh boundary, a fresh panel and an empty gift note
        <ErrorBoundary key={selectedIsbn} fallback={(e) => <p className="error" role="alert">Could not load details ({e.message})</p>}>
          <Suspense fallback={<p className="muted">Loading details…</p>}>
            <BookDetailsPanel isbn={selectedIsbn} onClose={() => setSelectedIsbn(null)} />
          </Suspense>
        </ErrorBoundary>
      )}
    </>
  );
}
