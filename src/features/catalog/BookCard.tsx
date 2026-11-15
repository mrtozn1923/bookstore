import { useState } from 'react';
import { Link } from 'react-router';
import type { BookSummary } from '@/features/catalog/types';
import { formatPrice } from '@/shared/format';

type BookCardProps = {
  book: BookSummary;
  quantityInCart: number;
  onAdd: (book: BookSummary, amount: number) => void;
};

const maxAmount = 10;

export function BookCard({ book, quantityInCart, onAdd }: BookCardProps) {
  // Local state: only this card cares how many copies are picked
  const [amount, setAmount] = useState(1);

  function handleAddClick() {
    onAdd(book, amount);
    setAmount(1);
  }

  return (
    <li className="book-card">
      <Link to={`/books/${book.isbn}`} className="title">{book.title}</Link>
      <span className="muted">
        {book.author} · {book.publishedYear}
        {book.publishedYear < 1900 && <span className="badge">Classic</span>}
      </span>
      <strong>{formatPrice(book.price)}</strong>
      <div className="amount">
        <button type="button" aria-label={`Fewer copies of ${book.title}`} disabled={amount === 1}
          onClick={() => setAmount((a) => a - 1)}>−</button>
        <span aria-live="polite">{amount}</span>
        <button type="button" aria-label={`More copies of ${book.title}`} disabled={amount === maxAmount}
          onClick={() => setAmount((a) => a + 1)}>+</button>
      </div>
      <button type="button" onClick={handleAddClick}>Add {amount} to cart</button>
      {quantityInCart > 0 && <span className="muted">In cart: {quantityInCart}</span>}
    </li>
  );
}
