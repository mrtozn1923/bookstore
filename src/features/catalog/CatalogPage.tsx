import { useQuery } from '@tanstack/react-query';
import { useRef } from 'react';
import { useSearchParams } from 'react-router';
import { useCart } from '@/features/cart';
import { BookList } from './BookList';
import { booksQuery, pageSize } from './catalogApi';
import { SearchBox } from './SearchBox';
import { RecentlyViewed } from '@/features/history';
import { useDebouncedValue } from '@/shared/useDebouncedValue';
import { useKeyboardShortcut } from '@/shared/useKeyboardShortcut';

export function CatalogPage() {
  // The search text and the page live in the URL: /?q=Dune&page=2 can be bookmarked and shared
  const [searchParams, setSearchParams] = useSearchParams();
  const query = searchParams.get('q') ?? '';
  const page = Number(searchParams.get('page') ?? '1');
  const { quantityOf, add } = useCart();
  const searchInput = useRef<HTMLInputElement>(null);
  useKeyboardShortcut('/', () => searchInput.current?.focus());

  const search = useDebouncedValue(query.trim(), 300);
  const { data, error, isFetching, isPlaceholderData } = useQuery(booksQuery(search, page));
  const pageCount = data ? Math.max(1, Math.ceil(data.total / pageSize)) : 1;

  function handleQueryChange(value: string) {
    // replace: typing should not add one history entry per letter; a new search starts on page 1
    setSearchParams(value ? { q: value } : {}, { replace: true });
  }

  function goToPage(next: number) {
    setSearchParams((params) => {
      params.set('page', String(next));
      return params;
    });
  }

  return (
    <>
      <h1>Catalog</h1>
      <RecentlyViewed />
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
            <BookList books={data.items} quantityOf={quantityOf} onAdd={add} />
          </div>
          <nav className="pager" aria-label="Pages">
            <button type="button" disabled={page === 1} onClick={() => goToPage(page - 1)}>Previous</button>
            <span>Page {page} of {pageCount}</span>
            <button type="button" disabled={page >= pageCount} onClick={() => goToPage(page + 1)}>Next</button>
          </nav>
        </>
      )}
    </>
  );
}
