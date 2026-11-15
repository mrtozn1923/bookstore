import type { BookSummary } from '@/features/catalog/types';

type BookCardProps = {
  book: BookSummary;
  quantity: number;
  onAdd: (isbn: string) => void;
};

const price = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'TRY', currencyDisplay: 'narrowSymbol' });

export function BookCard({ book, quantity, onAdd }: BookCardProps) {
  const label = quantity === 0 ? 'Add to cart' : `In cart: ${quantity}`;

  return (
    <li className="book-card">
      <strong>{book.title}</strong>
      <span className="muted">
        {book.author} · {book.publishedYear}
        {book.publishedYear < 1900 && <span className="badge">Classic</span>}
      </span>
      <strong>{price.format(book.price)}</strong>
      <button type="button" onClick={() => onAdd(book.isbn)}>{label}</button>
    </li>
  );
}
