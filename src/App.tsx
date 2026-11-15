import { useState, type ChangeEvent } from 'react';
import { CartSummary } from '@/features/cart/CartSummary';
import type { CartLine } from '@/features/cart/types';
import { BookList } from '@/features/catalog/BookList';
import { sampleBooks } from '@/features/catalog/sample-books';
import type { BookSummary } from '@/features/catalog/types';
import { Layout } from '@/shared/Layout';

type SortBy = 'title' | 'price';

export default function App() {
  const [lines, setLines] = useState<CartLine[]>([]);
  const [sortBy, setSortBy] = useState<SortBy>('title');
  const [lastAdded, setLastAdded] = useState<string | null>(null);

  // Derived values: recomputed on every render from state, never stored
  const books = sampleBooks.toSorted((a, b) =>
    sortBy === 'price' ? a.price - b.price : a.title.localeCompare(b.title));
  const quantities = Object.fromEntries(lines.map((line) => [line.isbn, line.quantity]));
  const itemCount = lines.reduce((sum, line) => sum + line.quantity, 0);
  const total = lines.reduce((sum, line) => sum + line.unitPrice * line.quantity, 0);

  function handleAdd(book: BookSummary, amount: number) {
    // Never mutate: build a new array (and a new object for the changed line)
    setLines((current) =>
      current.some((line) => line.isbn === book.isbn)
        ? current.map((line) => (line.isbn === book.isbn ? { ...line, quantity: line.quantity + amount } : line))
        : [...current, { isbn: book.isbn, title: book.title, unitPrice: book.price, quantity: amount }]);
    setLastAdded(`${amount} × ${book.title}`);
  }

  function handleSortChange(event: ChangeEvent<HTMLSelectElement>) {
    setSortBy(event.target.value as SortBy);
  }

  return (
    <Layout title="Bookstore" headerExtra={<CartSummary itemCount={itemCount} total={total} />}>
      <h1>Catalog</h1>
      <label>
        Sort by{' '}
        <select value={sortBy} onChange={handleSortChange}>
          <option value="title">Title</option>
          <option value="price">Price</option>
        </select>
      </label>
      {lastAdded && <p className="muted" role="status">Added {lastAdded}</p>}
      <BookList books={books} quantities={quantities} onAdd={handleAdd} />
    </Layout>
  );
}
