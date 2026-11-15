import { useState } from 'react';
import type { BookSummary } from '@/features/catalog/types';
import { formatPrice } from '@/shared/format';

type BookDetailsPanelProps = {
  book: BookSummary;
};

export function BookDetailsPanel({ book }: BookDetailsPanelProps) {
  // Belongs to one book; the parent resets it with key={book.isbn}
  const [giftNote, setGiftNote] = useState('');

  return (
    <section className="details" aria-label="Book details">
      <h2>{book.title}</h2>
      <p>{book.author}, {book.publishedYear} · {book.category} · {formatPrice(book.price)}</p>
      <label>
        Gift note{' '}
        <input value={giftNote} onChange={(e) => setGiftNote(e.target.value)} maxLength={120} />
      </label>
      <p className="muted">{120 - giftNote.length} characters left</p>
    </section>
  );
}
