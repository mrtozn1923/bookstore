import { useState } from 'react';
import { CartSummary } from '@/features/cart/CartSummary';
import { BookCard } from '@/features/catalog/BookCard';
import type { BookSummary } from '@/features/catalog/types';

const dune: BookSummary = {
  isbn: '9780441172719',
  title: 'Dune',
  author: 'Frank Herbert',
  category: 'Science Fiction',
  price: 489,
  publishedYear: 1965,
};

export default function App() {
  const [quantity, setQuantity] = useState(0);

  return (
    <main>
      <h1>Bookstore</h1>
      <CartSummary itemCount={quantity} />
      <ul className="book-grid">
        <BookCard book={dune} quantity={quantity} onAdd={() => setQuantity((q) => q + 1)} />
      </ul>
    </main>
  );
}
