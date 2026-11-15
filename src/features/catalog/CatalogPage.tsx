import { useState } from 'react';
import { BookDetailsPanel } from '@/features/catalog/BookDetailsPanel';
import { BookList } from '@/features/catalog/BookList';
import { sampleBooks } from '@/features/catalog/sample-books';
import { SearchBox } from '@/features/catalog/SearchBox';
import type { BookSummary } from '@/features/catalog/types';

type SortBy = 'title' | 'price';

type CatalogPageProps = {
  quantities: Record<string, number>;
  onAdd: (book: BookSummary, amount: number) => void;
};

export function CatalogPage({ quantities, onAdd }: CatalogPageProps) {
  // State that only the catalog needs stays here, not in App
  const [query, setQuery] = useState('');
  const [sortBy, setSortBy] = useState<SortBy>('title');
  // Store the id, not a copy of the book object
  const [selectedIsbn, setSelectedIsbn] = useState<string | null>(null);

  const term = query.trim().toLowerCase();
  const books = sampleBooks
    .filter((b) => b.title.toLowerCase().includes(term) || b.author.toLowerCase().includes(term))
    .toSorted((a, b) => (sortBy === 'price' ? a.price - b.price : a.title.localeCompare(b.title)));
  const selectedBook = sampleBooks.find((b) => b.isbn === selectedIsbn) ?? null;

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
      <p className="muted">{books.length} of {sampleBooks.length} books</p>
      <BookList books={books} quantities={quantities} onAdd={onAdd} onSelect={setSelectedIsbn} />
      {selectedBook && <BookDetailsPanel key={selectedBook.isbn} book={selectedBook} />}
    </>
  );
}
