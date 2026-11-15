import { useSuspenseQuery } from '@tanstack/react-query';
import { useId, useState } from 'react';
import { bookQuery } from '@/features/catalog/catalogApi';
import { formatPrice } from '@/shared/format';

type BookDetailsPanelProps = {
  isbn: string;
  onClose: () => void;
};

const maxNote = 120;

// Defined outside the component: the same function on every render, so React calls it only on attach
function revealPanel(node: HTMLElement) {
  const opener = document.activeElement;
  node.scrollIntoView({ block: 'nearest' });
  node.focus();
  // Cleanup (React 19): when the panel goes away with focus inside it, give focus back to the opener
  return () => {
    if (node.contains(document.activeElement) && opener instanceof HTMLElement) {
      opener.focus();
    }
  };
}

export function BookDetailsPanel({ isbn, onClose }: BookDetailsPanelProps) {
  // Suspends until the details are in the cache; errors go to the nearest error boundary
  const { data: book } = useSuspenseQuery(bookQuery(isbn));
  // Belongs to one book; the parent resets it with a key
  const [giftNote, setGiftNote] = useState('');
  // Unique per component instance, stable across renders: safe for id/htmlFor/aria-*
  const noteId = useId();

  return (
    <section ref={revealPanel} tabIndex={-1} className="details" aria-label="Book details">
      <h2>{book.title}</h2>
      <p>{book.author}, {book.publishedYear} · {book.category} · {formatPrice(book.price)}</p>
      <p>{book.description}</p>
      <p className="muted">{book.available > 0 ? `${book.available} in stock` : 'Out of stock'}</p>
      <label htmlFor={noteId}>Gift note</label>{' '}
      <input id={noteId} aria-describedby={`${noteId}-hint`} value={giftNote}
        onChange={(e) => setGiftNote(e.target.value)} maxLength={maxNote} />
      <p id={`${noteId}-hint`} className="muted">{maxNote - giftNote.length} characters left</p>
      <button type="button" onClick={onClose}>Close</button>
    </section>
  );
}
