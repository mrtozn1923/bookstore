import { useQuery, useSuspenseQuery } from '@tanstack/react-query';
import { useId, useState } from 'react';
import { Link, useParams } from 'react-router';
import { useCart } from '@/features/cart';
import { bookQuery, booksQuery } from './catalogApi';
import type { BookDetails } from './types';
import { formatPrice } from '@/shared/format';

export function BookPage() {
  const { isbn } = useParams();
  const { data: book } = useSuspenseQuery(bookQuery(isbn!));
  // Same route, new :isbn → React keeps this component; the key gives every book its own form state
  return <BookDetailsView key={book.isbn} book={book} />;
}

const maxNote = 120;

function BookDetailsView({ book }: { book: BookDetails }) {
  const [giftNote, setGiftNote] = useState('');
  const noteId = useId();
  const { add, quantityOf } = useCart();
  // Other books by the same author (the API searches titles and author names)
  const { data: sameAuthor } = useQuery(booksQuery(book.author, 1));
  const others = sameAuthor?.items.filter((b) => b.isbn !== book.isbn) ?? [];

  return (
    <section className="details" aria-label="Book details">
      {/* React 19 document metadata: <title> is moved into <head> */}
      <title>{`${book.title} · Bookstore`}</title>
      <p><Link to="/">← Back to the catalog</Link></p>
      <h1>{book.title}</h1>
      <p>{book.author}, {book.publishedYear} · {book.category} · {formatPrice(book.price)}</p>
      <p>{book.description}</p>
      <p className="muted">{book.available > 0 ? `${book.available} in stock` : 'Out of stock'}</p>
      <button type="button" disabled={book.available === 0} onClick={() => add(book, 1)}>
        Add to cart{quantityOf(book.isbn) > 0 ? ` (${quantityOf(book.isbn)} in cart)` : ''}
      </button>
      <p>
        <label htmlFor={noteId}>Gift note</label>{' '}
        <input id={noteId} value={giftNote} onChange={(e) => setGiftNote(e.target.value)} maxLength={maxNote} />
      </p>
      {others.length > 0 && (
        <>
          <h2>More by {book.author}</h2>
          <ul>
            {others.map((b) => <li key={b.isbn}><Link to={`/books/${b.isbn}`}>{b.title}</Link></li>)}
          </ul>
        </>
      )}
    </section>
  );
}
