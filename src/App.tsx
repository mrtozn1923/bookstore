import { useState } from 'react';
import { BookCard, type BookSummary } from './BookCard.tsx';

const dune: BookSummary = {
  isbn: '9780441172719',
  title: 'Dune',
  author: 'Frank Herbert',
  category: 'Science Fiction',
  price: 489,
  publishedYear: 1965,
};

export default function App() {
  // State: the only value here that changes over time
  const [quantity, setQuantity] = useState(0);

  return (
    <main>
      <h1>Bookstore</h1>
      <p className="muted">Cart: {quantity} item(s)</p>
      <ul className="book-grid">
        <BookCard book={dune} quantity={quantity} onAdd={() => setQuantity((q) => q + 1)} />
      </ul>
    </main>
  );
}
