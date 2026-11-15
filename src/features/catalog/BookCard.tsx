import { useState } from 'react';
import type { BookSummary } from '@/features/catalog/types';
import { formatPrice } from '@/shared/format';

type BookCardProps = {
  book: BookSummary;
  quantityInCart: number;
  onAdd: (book: BookSummary, amount: number) => void;
  onSelect: (isbn: string) => void;
};

const maxAmount = 10;

export function BookCard({ book, quantityInCart, onAdd, onSelect }: BookCardProps) {
  // Local state: only this card cares how many copies are picked
  const [amount, setAmount] = useState(1);

  function handleAddClick() {
    onAdd(book, amount);
    setAmount(1);
  }

  return (
    <li className="book-card">
      <button type="button" className="link" onClick={() => onSelect(book.isbn)}>{book.title}</button>
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
