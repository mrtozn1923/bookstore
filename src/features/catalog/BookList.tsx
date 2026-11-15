import { BookCard } from '@/features/catalog/BookCard';
import type { BookSummary } from '@/features/catalog/types';

type BookListProps = {
  books: BookSummary[];
  quantities: Record<string, number>;
  onAdd: (book: BookSummary, amount: number) => void;
};

export function BookList({ books, quantities, onAdd }: BookListProps) {
  if (books.length === 0) {
    return <p className="muted">No books match your search.</p>;
  }

  return (
    <ul className="book-grid">
      {books.map((book) => (
        // key: a stable identity like @for's track; never the array index
        <BookCard key={book.isbn} book={book} quantityInCart={quantities[book.isbn] ?? 0} onAdd={onAdd} />
      ))}
    </ul>
  );
}
