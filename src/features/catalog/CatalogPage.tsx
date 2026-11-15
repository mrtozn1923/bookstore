import { useRef, useState } from 'react';
import { useCart } from '@/features/cart/CartContext';
import { BookDetailsPanel } from '@/features/catalog/BookDetailsPanel';
import { BookList } from '@/features/catalog/BookList';
import { SearchBox } from '@/features/catalog/SearchBox';
import { useBookSearch } from '@/features/catalog/useBookSearch';
import { useKeyboardShortcut } from '@/shared/useKeyboardShortcut';

type SortBy = 'title' | 'price';

export function CatalogPage() {
  const [query, setQuery] = useState('');
  const [sortBy, setSortBy] = useState<SortBy>('title');
  const [selectedIsbn, setSelectedIsbn] = useState<string | null>(null);
  const { lines, itemCount, add } = useCart();
  // Like @ViewChild: a handle to the <input> DOM node, filled in after commit
  const searchInput = useRef<HTMLInputElement>(null);

  const { result, isLoading } = useBookSearch(query, (searched, total) => {
    console.info(`[search] "${searched}" -> ${total} book(s), cart has ${itemCount} item(s)`);
  });
  useKeyboardShortcut('/', () => searchInput.current?.focus());
  // Always called; the condition lives inside the callback, not around the hook
  useKeyboardShortcut('Escape', () => setSelectedIsbn(null));

  const quantities = Object.fromEntries(lines.map((line) => [line.isbn, line.quantity]));
  const books = result?.status === 'success'
    ? result.books.toSorted((a, b) => (sortBy === 'price' ? a.price - b.price : a.title.localeCompare(b.title)))
    : [];
  const selectedBook = books.find((b) => b.isbn === selectedIsbn) ?? null;

  return (
    <>
      <h1>Catalog</h1>
      <div className="toolbar">
        <SearchBox ref={searchInput} value={query} onChange={setQuery} />
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
        <BookList books={books} quantities={quantities} onAdd={add} onSelect={setSelectedIsbn} />
      )}
      {selectedBook && <BookDetailsPanel key={selectedBook.isbn} book={selectedBook} onClose={() => setSelectedIsbn(null)} />}
    </>
  );
}
